using System.Security.Claims;
using System.Text.Json;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAlexController : ControllerBase
    {
        private readonly IOpenAlexService _openAlexService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<OpenAlexController> _logger;

        public OpenAlexController(
            IOpenAlexService openAlexService,
            IAuditLogService auditLogService,
            ILogger<OpenAlexController> logger)
        {
            _openAlexService = openAlexService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy preview metadata của một OpenAlex Work để Researcher auto-fill form Paper.
        /// Endpoint này chỉ đọc metadata và không ghi vào Papers.
        /// </summary>
        /// <param name="workId">Canonical OpenAlex Work ID dạng W + chữ số.</param>
        [HttpGet("works/{workId}")]
        [Authorize(Policy = "AuthenticatedUser")]
        [EnableRateLimiting("OpenAlexWorkLookup")]
        public async Task<ActionResult<OpenAlexWorkPreviewResponse>> GetWorkPreview(
            string workId,
            CancellationToken cancellationToken)
        {
            if (!IsCanonicalWorkId(workId))
            {
                var invalid = new OpenAlexWorkPreviewResponse
                {
                    OpenAlexWorkId = workId?.Trim() ?? string.Empty,
                    LookupStatus = "InvalidWorkId",
                    SourceFetchedAt = DateTime.UtcNow,
                    Message = "The supplied OpenAlex Work ID must be a canonical W-prefixed ID."
                };

                await WriteLookupAuditAsync(
                    workId?.Trim() ?? string.Empty,
                    invalid,
                    cancellationToken);

                return BadRequest(invalid);
            }

            var result = await _openAlexService
                .GetWorkPreviewByIdAsync(
                    workId,
                    cancellationToken);

            await WriteLookupAuditAsync(
                workId,
                result,
                cancellationToken);

            if (result.RetryAfterSeconds.HasValue &&
                result.RetryAfterSeconds.Value > 0)
            {
                Response.Headers["Retry-After"] =
                    result.RetryAfterSeconds.Value.ToString();
            }

            return result.LookupStatus switch
            {
                "Found" => Ok(result),
                "InvalidWorkId" => BadRequest(result),
                "NotFound" => NotFound(result),
                "RateLimited" => StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    result),
                "ProviderTimeout" => StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    result),
                "ProviderUnavailable" => StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    result),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway,
                    result)
            };
        }

        private async Task WriteLookupAuditAsync(
            string workId,
            OpenAlexWorkPreviewResponse result,
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var actorId))
            {
                return;
            }

            var actorName =
                User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? $"User {actorId}";

            var details = JsonSerializer.Serialize(
                new
                {
                    Provider = "OpenAlex",
                    Outcome = result.LookupStatus,
                    result.SourceFetchedAt
                });

            try
            {
                await _auditLogService.CreateAsync(
                    new AuditLogCreateRequest
                    {
                        AdminId = actorId,
                        AdminName = actorName,
                        Action = "OPENALEX_WORK_LOOKUP",
                        Target = "OpenAlexWork",
                        TargetId = workId,
                        Details = details
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to write OpenAlex work lookup audit for Work ID {WorkId}.",
                    workId);
            }
        }

        private static bool IsCanonicalWorkId(string? workId)
        {
            if (string.IsNullOrWhiteSpace(workId) ||
                workId.Length < 2 ||
                workId[0] != 'W')
            {
                return false;
            }

            for (var index = 1; index < workId.Length; index++)
            {
                if (!char.IsDigit(workId[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
