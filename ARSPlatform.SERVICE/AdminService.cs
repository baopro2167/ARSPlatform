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
            var roleRequest = await _roleRequestRepository
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.RequestedRole)
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.RoleRequestId == request.RoleRequestId,
                    cancellationToken);

            if (roleRequest == null)
            {
                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    null,
                    string.Empty,
                    "RoleRequestNotFound",
                    "NotCalled",
                    correlationId);

                return (
                    new OrcidLookupResponse
                    {
                        LookupStatus = "RoleRequestNotFound",
                        Message = "Role request not found."
                    },
                    404);
            }

            var targetUserId = roleRequest.UserId;
            var storedOrcid = roleRequest.User.OrcidId;

            if (string.IsNullOrWhiteSpace(storedOrcid))
            {
                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    targetUserId,
                    string.Empty,
                    "OrcidNotConnected",
                    "NotCalled",
                    correlationId);

                return (
                    new OrcidLookupResponse
                    {
                        LookupStatus = "OrcidNotConnected",
                        Message = "This ARS account has not connected an ORCID iD."
                    },
                    409);
            }

            if (!OrcidIdUtility.TryNormalizeAndValidate(
                    storedOrcid,
                    out var normalizedOrcidId))
            {
                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    targetUserId,
                    storedOrcid.Trim(),
                    "StoredOrcidInvalid",
                    "NotCalled",
                    correlationId);

                return (
                    new OrcidLookupResponse
                    {
                        OrcidId = storedOrcid.Trim(),
                        LookupStatus = "StoredOrcidInvalid",
                        Message = "The ARS account contains an invalid ORCID iD."
                    },
                    409);
            }

            if (!roleRequest.User.IsOrcidVerified)
            {
                await WriteAuditAsync(
                    adminId,
                    adminName,
                    request.RoleRequestId,
                    targetUserId,
                    normalizedOrcidId,
                    "OrcidNotVerified",
                    "NotCalled",
                    correlationId);

                return (
                    new OrcidLookupResponse
                    {
                        OrcidId = normalizedOrcidId,
                        LookupStatus = "OrcidNotVerified",
                        Message = "The ORCID iD connected to this ARS account has not been verified through ORCID OAuth."
                    },
                    409);
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
                    lookupResult.LookupStatus),
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

            return (
                lookupResult,
                statusCode);
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