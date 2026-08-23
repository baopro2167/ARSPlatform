using System.Security.Claims;
using System.Text.Json;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IOpenAlexService _openAlexService;
        private readonly IRoleRequestRepository _roleRequestRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public AdminController(
            IOpenAlexService openAlexService,
            IRoleRequestRepository roleRequestRepository,
            IAuditLogRepository auditLogRepository)
        {
            _openAlexService = openAlexService;
            _roleRequestRepository = roleRequestRepository;
            _auditLogRepository = auditLogRepository;
        }

        [HttpPost("orcid-lookup")]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status502BadGateway)]
        [ProducesResponseType(
            typeof(OrcidLookupResponse),
            StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> LookupOrcid(
            [FromBody] OrcidLookupRequest? request,
            CancellationToken cancellationToken)
        {
            var adminContext = GetCurrentAdmin();

            if (adminContext == null)
            {
                return Unauthorized();
            }

            var adminId = adminContext.Value.AdminId;
            var adminName = adminContext.Value.AdminName;

            if (request == null)
            {
                await WriteAuditAsync(
                    adminId,
                    adminName,
                    null,
                    null,
                    string.Empty,
                    "InvalidRequest",
                    "NotCalled");

                return BadRequest(new
                {
                    Message = "Request body is required."
                });
            }

            if (!OrcidIdUtility.TryNormalizeAndValidate(
                    request.OrcidId,
                    out var normalizedOrcidId))
            {
                var result =
                    await _openAlexService.LookupByOrcidAsync(
                        request.OrcidId,
                        cancellationToken);

                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    null,
                    request.OrcidId?.Trim() ?? string.Empty,
                    result.LookupStatus,
                    "NotCalled");

                return MapLookupResult(result);
            }

            int? targetUserId = null;

            if (request.RoleRequestId.HasValue)
            {
                var roleRequest =
                    await _roleRequestRepository
                        .GetQueryable()
                        .AsNoTracking()
                        .Include(x => x.RequestedRole)
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(
                            x =>
                                x.RoleRequestId ==
                                request.RoleRequestId.Value,
                            cancellationToken);

                if (roleRequest == null)
                {
                    await WriteAuditAsync(
                        adminId,
                        adminName,
                        request.RoleRequestId,
                        null,
                        normalizedOrcidId,
                        "RoleRequestNotFound",
                        "NotCalled");

                    return NotFound(new
                    {
                        Message = "Role request not found."
                    });
                }

                targetUserId = roleRequest.UserId;

                if (!string.Equals(
                        roleRequest.RequestedRole.Name,
                        "Reviewer",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await WriteAuditAsync(
                        adminId,
                        adminName,
                        request.RoleRequestId,
                        targetUserId,
                        normalizedOrcidId,
                        "RoleRequestIsNotReviewer",
                        "NotCalled");

                    return BadRequest(new
                    {
                        Message =
                            "ORCID lookup is only available for Reviewer role requests."
                    });
                }

                if (string.IsNullOrWhiteSpace(
                        roleRequest.User.OrcidId))
                {
                    await WriteAuditAsync(
                        adminId,
                        adminName,
                        request.RoleRequestId,
                        targetUserId,
                        normalizedOrcidId,
                        "StoredOrcidMissing",
                        "NotCalled");

                    return Conflict(new
                    {
                        Message =
                            "The Reviewer account does not contain an ORCID iD."
                    });
                }

                if (!OrcidIdUtility.TryNormalizeAndValidate(
                        roleRequest.User.OrcidId,
                        out var storedOrcidId))
                {
                    await WriteAuditAsync(
                        adminId,
                        adminName,
                        request.RoleRequestId,
                        targetUserId,
                        normalizedOrcidId,
                        "StoredOrcidInvalid",
                        "NotCalled");

                    return Conflict(new
                    {
                        Message =
                            "The Reviewer account contains an invalid ORCID iD."
                    });
                }

                if (!string.Equals(
                        storedOrcidId,
                        normalizedOrcidId,
                        StringComparison.Ordinal))
                {
                    await WriteAuditAsync(
                        adminId,
                        adminName,
                        request.RoleRequestId,
                        targetUserId,
                        normalizedOrcidId,
                        "OrcidMismatch",
                        "NotCalled");

                    return Conflict(new
                    {
                        Message =
                            "The supplied ORCID iD does not match the ORCID stored for this Reviewer role request."
                    });
                }
            }

            var lookupResult =
                await _openAlexService.LookupByOrcidAsync(
                    normalizedOrcidId,
                    cancellationToken);

            await WriteAuditAsync(
                adminId,
                adminName,
                request.RoleRequestId,
                targetUserId,
                normalizedOrcidId,
                lookupResult.LookupStatus,
                GetProviderStatus(
                    lookupResult.LookupStatus));

            return MapLookupResult(
                lookupResult);
        }

        private (int AdminId, string AdminName)? GetCurrentAdmin()
        {
            var adminIdValue =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)
                    ?.Value;

            if (!int.TryParse(
                    adminIdValue,
                    out var adminId))
            {
                return null;
            }

            var adminName =
                User.FindFirst(
                    ClaimTypes.Name)
                    ?.Value;

            if (string.IsNullOrWhiteSpace(
                    adminName))
            {
                adminName =
                    $"Admin {adminId}";
            }

            return (
                adminId,
                adminName);
        }

        private async Task WriteAuditAsync(
            int adminId,
            string adminName,
            int? roleRequestId,
            int? userId,
            string orcidId,
            string outcome,
            string providerStatus)
        {
            var correlationId =
                HttpContext.TraceIdentifier;

            var details =
                JsonSerializer.Serialize(
                    new
                    {
                        OrcidId = orcidId,
                        RoleRequestId = roleRequestId,
                        UserId = userId,
                        Outcome = outcome,
                        Provider = "OpenAlex",
                        ProviderStatus = providerStatus,
                        CorrelationId = correlationId
                    });

            var auditLog =
                new AuditLog
                {
                    AdminId = adminId,
                    AdminName = adminName,
                    Action = "CHECK_ORCID",
                    Target =
                        roleRequestId.HasValue
                            ? "RoleRequest"
                            : "ORCID",
                    TargetId =
                        roleRequestId.HasValue
                            ? roleRequestId.Value.ToString()
                            : orcidId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

            await _auditLogRepository.AddAsync(
                auditLog);

            await _auditLogRepository
                .SaveChangesAsync();
        }

        private static string GetProviderStatus(
            string lookupStatus)
        {
            return lookupStatus switch
            {
                "Found" =>
                    "Success",

                "NotFound" =>
                    "NotFound",

                "RateLimited" =>
                    "RateLimited",

                "ProviderUnavailable" =>
                    "Unavailable",

                "ProviderError" =>
                    "Error",

                "InvalidOrcid" =>
                    "NotCalled",

                _ =>
                    "Unknown"
            };
        }

        private IActionResult MapLookupResult(
            OrcidLookupResponse result)
        {
            return result.LookupStatus switch
            {
                "Found" =>
                    Ok(result),

                "InvalidOrcid" =>
                    BadRequest(result),

                "NotFound" =>
                    NotFound(result),

                "RateLimited" =>
                    StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        result),

                "ProviderUnavailable" =>
                    StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        result),

                "ProviderError" =>
                    StatusCode(
                        StatusCodes.Status502BadGateway,
                        result),

                _ =>
                    StatusCode(
                        StatusCodes.Status502BadGateway,
                        result)
            };
        }
    }
}