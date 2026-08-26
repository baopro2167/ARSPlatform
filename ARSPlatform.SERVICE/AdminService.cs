using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class AdminService : IAdminService
    {
        private readonly IOpenAlexService _openAlexService;
        private readonly IRoleRequestRepository _roleRequestRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public AdminService(
            IOpenAlexService openAlexService,
            IRoleRequestRepository roleRequestRepository,
            IAuditLogRepository auditLogRepository)
        {
            _openAlexService = openAlexService;
            _roleRequestRepository = roleRequestRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<(OrcidLookupResponse Response, int StatusCode)> LookupOrcidAsync(
            OrcidLookupRequest request,
            int adminId,
            string adminName,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (!OrcidIdUtility.TryNormalizeAndValidate(request.OrcidId, out var normalizedOrcidId))
            {
                var invalidResult = await _openAlexService.LookupByOrcidAsync(request.OrcidId, cancellationToken);

                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    null,
                    request.OrcidId?.Trim() ?? string.Empty,
                    invalidResult.LookupStatus,
                    "NotCalled",
                    correlationId);

                return (invalidResult, 400);
            }

            int? targetUserId = null;

            if (request.RoleRequestId.HasValue)
            {
                var roleRequest = await _roleRequestRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(x => x.RequestedRole)
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.RoleRequestId == request.RoleRequestId.Value, cancellationToken);

                if (roleRequest == null)
                {
                    await WriteAuditAsync(adminId, adminName, request.RoleRequestId, null, normalizedOrcidId, "RoleRequestNotFound", "NotCalled", correlationId);
                    return (new OrcidLookupResponse { LookupStatus = "RoleRequestNotFound", Message = "Role request not found." }, 404);
                }

                targetUserId = roleRequest.UserId;

                if (!string.Equals(roleRequest.RequestedRole.Name, "Reviewer", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteAuditAsync(adminId, adminName, request.RoleRequestId, targetUserId, normalizedOrcidId, "RoleRequestIsNotReviewer", "NotCalled", correlationId);
                    return (new OrcidLookupResponse { LookupStatus = "RoleRequestIsNotReviewer", Message = "ORCID lookup is only available for Reviewer role requests." }, 400);
                }

                if (string.IsNullOrWhiteSpace(roleRequest.User.OrcidId))
                {
                    await WriteAuditAsync(adminId, adminName, request.RoleRequestId, targetUserId, normalizedOrcidId, "StoredOrcidMissing", "NotCalled", correlationId);
                    return (new OrcidLookupResponse { LookupStatus = "StoredOrcidMissing", Message = "The Reviewer account does not contain an ORCID iD." }, 409);
                }

                if (!OrcidIdUtility.TryNormalizeAndValidate(roleRequest.User.OrcidId, out var storedOrcidId))
                {
                    await WriteAuditAsync(adminId, adminName, request.RoleRequestId, targetUserId, normalizedOrcidId, "StoredOrcidInvalid", "NotCalled", correlationId);
                    return (new OrcidLookupResponse { LookupStatus = "StoredOrcidInvalid", Message = "The Reviewer account contains an invalid ORCID iD." }, 409);
                }

                if (!string.Equals(storedOrcidId, normalizedOrcidId, StringComparison.Ordinal))
                {
                    await WriteAuditAsync(adminId, adminName, request.RoleRequestId, targetUserId, normalizedOrcidId, "OrcidMismatch", "NotCalled", correlationId);
                    return (new OrcidLookupResponse { LookupStatus = "OrcidMismatch", Message = "The supplied ORCID iD does not match the ORCID stored for this Reviewer role request." }, 409);
                }
            }

            var lookupResult = await _openAlexService.LookupByOrcidAsync(normalizedOrcidId, cancellationToken);

            await WriteAuditAsync(
                adminId,
                adminName,
                request.RoleRequestId,
                targetUserId,
                normalizedOrcidId,
                lookupResult.LookupStatus,
                GetProviderStatus(lookupResult.LookupStatus),
                correlationId);

            var statusCode = lookupResult.LookupStatus switch
            {
                "Found" => 200,
                "InvalidOrcid" => 400,
                "NotFound" => 404,
                "RateLimited" => 429,
                "ProviderUnavailable" => 503,
                "ProviderError" => 502,
                _ => 502
            };

            return (lookupResult, statusCode);
        }

        private async Task WriteAuditAsync(
            int adminId,
            string adminName,
            int? roleRequestId,
            int? userId,
            string orcidId,
            string outcome,
            string providerStatus,
            string correlationId)
        {
            var details = JsonSerializer.Serialize(new
            {
                OrcidId = orcidId,
                RoleRequestId = roleRequestId,
                UserId = userId,
                Outcome = outcome,
                Provider = "OpenAlex",
                ProviderStatus = providerStatus,
                CorrelationId = correlationId
            });

            var auditLog = new AuditLog
            {
                AdminId = adminId,
                AdminName = adminName,
                Action = "CHECK_ORCID",
                Target = roleRequestId.HasValue ? "RoleRequest" : "ORCID",
                TargetId = roleRequestId.HasValue ? roleRequestId.Value.ToString() : orcidId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
            await _auditLogRepository.SaveChangesAsync();
        }

        private static string GetProviderStatus(string lookupStatus)
        {
            return lookupStatus switch
            {
                "Found" => "Success",
                "NotFound" => "NotFound",
                "RateLimited" => "RateLimited",
                "ProviderUnavailable" => "Unavailable",
                "ProviderError" => "Error",
                "InvalidOrcid" => "NotCalled",
                _ => "Unknown"
            };
        }
    }
}
