using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ARSPlatform.API.CONTROLLER
{
    /// <summary>
    /// API tích hợp Semantic Scholar Graph API (free public API).
    /// Cho phép Researcher lookup author profile, search theo tên,
    /// và lấy danh sách bài báo của một tác giả.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ScholarController : ControllerBase
    {
        private readonly ISemanticScholarService _scholarService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ScholarController> _logger;

        public ScholarController(
            ISemanticScholarService scholarService,
            IAuditLogService auditLogService,
            ILogger<ScholarController> logger)
        {
            _scholarService = scholarService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy profile + chỉ số trích dẫn (paperCount, citationCount, hIndex, affiliations)
        /// của một tác giả theo Semantic Scholar Author ID.
        /// </summary>
        /// <param name="authorId">Semantic Scholar Author ID (vd: "1741101").</param>
        [HttpGet("author/{authorId}")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<ActionResult<ScholarAuthorProfileResponse>> GetAuthor(
            string authorId,
            CancellationToken cancellationToken)
        {
            var result = await _scholarService
                .GetAuthorAsync(authorId, cancellationToken);

            await WriteAuditAsync(
                action: "SCHOLAR_AUTHOR_LOOKUP",
                target: "ScholarAuthor",
                targetId: result.AuthorId,
                lookupStatus: result.LookupStatus,
                sourceFetchedAt: result.SourceFetchedAt,
                cancellationToken: cancellationToken);

            ApplyRetryAfterHeader(result.RetryAfterSeconds);

            return result.LookupStatus switch
            {
                ScholarLookupStatus.Found => Ok(result),
                ScholarLookupStatus.InvalidAuthorId => BadRequest(result),
                ScholarLookupStatus.NotFound => NotFound(result),
                ScholarLookupStatus.RateLimited => StatusCode(
                    StatusCodes.Status429TooManyRequests, result),
                ScholarLookupStatus.ProviderTimeout => StatusCode(
                    StatusCodes.Status504GatewayTimeout, result),
                ScholarLookupStatus.ProviderUnavailable => StatusCode(
                    StatusCodes.Status503ServiceUnavailable, result),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway, result)
            };
        }

        /// <summary>
        /// Tìm kiếm tác giả theo tên.
        /// Trả về tối đa <paramref name="limit"/> kết quả (1-1000).
        /// </summary>
        [HttpGet("authors/search")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<ActionResult<ScholarSearchResponse>> SearchAuthors(
            [FromQuery] string? query,
            [FromQuery] int limit = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _scholarService
                .SearchAuthorsAsync(
                    query ?? string.Empty,
                    limit,
                    cancellationToken);

            await WriteAuditAsync(
                action: "SCHOLAR_AUTHOR_SEARCH",
                target: "ScholarAuthor",
                targetId: result.Query,
                lookupStatus: result.LookupStatus,
                sourceFetchedAt: result.SourceFetchedAt,
                cancellationToken: cancellationToken);

            ApplyRetryAfterHeader(result.RetryAfterSeconds);

            return result.LookupStatus switch
            {
                ScholarLookupStatus.Found => Ok(result),
                ScholarLookupStatus.InvalidQuery => BadRequest(result),
                ScholarLookupStatus.RateLimited => StatusCode(
                    StatusCodes.Status429TooManyRequests, result),
                ScholarLookupStatus.ProviderTimeout => StatusCode(
                    StatusCodes.Status504GatewayTimeout, result),
                ScholarLookupStatus.ProviderUnavailable => StatusCode(
                    StatusCodes.Status503ServiceUnavailable, result),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway, result)
            };
        }

        /// <summary>
        /// Lấy danh sách bài báo của một tác giả theo Author ID.
        /// Trả về tối đa <paramref name="limit"/> bài (1-1000).
        /// </summary>
        [HttpGet("author/{authorId}/papers")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<ActionResult<ScholarAuthorPapersResponse>> GetAuthorPapers(
            string authorId,
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await _scholarService
                .GetAuthorPapersAsync(authorId, limit, cancellationToken);

            await WriteAuditAsync(
                action: "SCHOLAR_AUTHOR_PAPERS",
                target: "ScholarAuthor",
                targetId: result.AuthorId,
                lookupStatus: result.LookupStatus,
                sourceFetchedAt: result.SourceFetchedAt,
                cancellationToken: cancellationToken);

            ApplyRetryAfterHeader(result.RetryAfterSeconds);

            return result.LookupStatus switch
            {
                ScholarLookupStatus.Found => Ok(result),
                ScholarLookupStatus.InvalidAuthorId => BadRequest(result),
                ScholarLookupStatus.NotFound => NotFound(result),
                ScholarLookupStatus.RateLimited => StatusCode(
                    StatusCodes.Status429TooManyRequests, result),
                ScholarLookupStatus.ProviderTimeout => StatusCode(
                    StatusCodes.Status504GatewayTimeout, result),
                ScholarLookupStatus.ProviderUnavailable => StatusCode(
                    StatusCodes.Status503ServiceUnavailable, result),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway, result)
            };
        }

        // ===== Helpers =====

        private void ApplyRetryAfterHeader(int? retryAfterSeconds)
        {
            if (retryAfterSeconds.HasValue && retryAfterSeconds.Value > 0)
            {
                Response.Headers["Retry-After"] =
                    retryAfterSeconds.Value.ToString();
            }
        }

        private async Task WriteAuditAsync(
            string action,
            string target,
            string? targetId,
            string lookupStatus,
            DateTime sourceFetchedAt,
            CancellationToken cancellationToken)
        {
            var userIdValue = User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

            if (!int.TryParse(userIdValue, out var actorId))
            {
                return;
            }

            var actorName =
                User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? $"User {actorId}";

            var details = JsonSerializer.Serialize(new
            {
                Provider = "SemanticScholar",
                Outcome = lookupStatus,
                SourceFetchedAt = sourceFetchedAt
            });

            try
            {
                await _auditLogService.CreateAsync(
                    new AuditLogCreateRequest
                    {
                        AdminId = actorId,
                        AdminName = actorName,
                        Action = action,
                        Target = target,
                        TargetId = targetId,
                        Details = details
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to write Semantic Scholar audit for {Action} target {TargetId}.",
                    action,
                    targetId);
            }
        }
    }
}