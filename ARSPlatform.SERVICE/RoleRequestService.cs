using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.SERVICES
{
    public class RoleRequestService : IRoleRequestService
    {
        private readonly IRoleRequestRepository _roleRequestRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IProfessionalProfileRepository _professionalProfileRepository;
        private readonly INotificationRepository _notificationRepository;

        public RoleRequestService(
            IRoleRequestRepository roleRequestRepository,
            IUserRoleRepository userRoleRepository,
            IProfessionalProfileRepository professionalProfileRepository,
            INotificationRepository notificationRepository)
        {
            _roleRequestRepository = roleRequestRepository;
            _userRoleRepository = userRoleRepository;
            _professionalProfileRepository = professionalProfileRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<IEnumerable<RoleRequestResponse>> GetAllAsync()
        {
            var roleRequests = await _roleRequestRepository
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .Include(x => x.RequestedRole)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return roleRequests.Select(MapResponse).ToList();
        }

        public async Task<PagedResult<RoleRequestResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var query = _roleRequestRepository
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .Include(x => x.RequestedRole)
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageNumber = paginationParams.PageNumber < 1 ? 1 : paginationParams.PageNumber;
            var pageSize = paginationParams.PageSize < 1 ? 10 : paginationParams.PageSize;

            var roleRequests = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = roleRequests.Select(MapResponse).ToList();
            return new PagedResult<RoleRequestResponse>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResult<RoleRequestResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var query = _roleRequestRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .Include(x => x.RequestedRole)
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();
            var page = pageNumber < 1 ? 1 : pageNumber;
            var size = pageSize < 1 ? 10 : pageSize;

            var roleRequests = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var dtos = roleRequests.Select(MapResponse).ToList();
            return new PagedResult<RoleRequestResponse>(dtos, totalCount, page, size);
        }

        public async Task<PagedResult<RoleRequestResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<RoleRequestResponse?> GetByIdAsync(int id)
        {
            var roleRequest = await _roleRequestRepository
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .Include(x => x.RequestedRole)
                .FirstOrDefaultAsync(x => x.RoleRequestId == id);

            return roleRequest == null
                ? null
                : MapResponse(roleRequest);
        }

        public async Task<RoleRequestResponse> ApproveAsync(
            int id,
            int adminId,
            RoleRequestDecisionRequest request)
        {
            var roleRequest = await LoadForDecisionAsync(id);

            if (roleRequest == null)
            {
                throw new KeyNotFoundException(
                    $"Role request {id} was not found.");
            }

            EnsurePending(roleRequest);
            ValidateNotes(request.Notes, required: false);

            var user = roleRequest.User;
            var requestedRole = roleRequest.RequestedRole;
            var now = DateTime.UtcNow;

            if (string.Equals(
        requestedRole.Name,
        "Reviewer",
        StringComparison.OrdinalIgnoreCase))
            {
                if (!OrcidIdUtility.TryNormalizeAndValidate(
                        user.OrcidId,
                        out var normalizedOrcidId))
                {
                    throw new InvalidOperationException(
                        "Reviewer role requests cannot be approved because the user does not have a valid ORCID iD.");
                }

                user.OrcidId = normalizedOrcidId;
            }

            var roleAlreadyAssigned = user.UserRoles.Any(
                x => x.RoleId == requestedRole.RoleId);

            if (!roleAlreadyAssigned)
            {
                var userRole = new UserRole
                {
                    User = user,
                    Role = requestedRole,
                    UserId = user.UserId,
                    RoleId = requestedRole.RoleId,
                    UserRole1 = requestedRole.Name,
                    CreatedAt = now
                };

                await _userRoleRepository.AddAsync(userRole);
            }

            var professionalProfile = await _professionalProfileRepository
                .GetByIdAsync(user.UserId);

            if (professionalProfile == null)
            {
                professionalProfile = new ProfessionalProfile
                {
                    UserId = user.UserId,
                    User = user,
                    OrcidId = user.OrcidId,
                    Hindex = 0,
                    TotalCitations = 0,
                    PublicationCount = 0,
                    SyncStatus = "pending",
                    UpdatedAt = now
                };

                await _professionalProfileRepository.AddAsync(
                    professionalProfile);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(professionalProfile.OrcidId)
                    && !string.IsNullOrWhiteSpace(user.OrcidId))
                {
                    professionalProfile.OrcidId = user.OrcidId;
                }

                professionalProfile.UpdatedAt = now;
            }

            if (string.Equals(
                    roleRequest.RequestType,
                    "INITIAL_REGISTRATION",
                    StringComparison.OrdinalIgnoreCase))
            {
                user.IsActive = true;
                user.VerificationStatus = "Accepted";
                user.UpdatedAt = now;
            }

            roleRequest.Status = "APPROVED";
            roleRequest.Notes = NormalizeNotes(request.Notes);
            roleRequest.ReviewedByAdminId = adminId;
            roleRequest.ReviewedAt = now;
            roleRequest.UpdatedAt = now;

            try
            {
                var notification = new Notification
                {
                    UserId = user.UserId,
                    Message = $"Yêu cầu xét duyệt vai trò \"{requestedRole.Name}\" của bạn đã được phê duyệt thành công.",
                    IsRead = false,
                    CreatedAt = now
                };
                await _notificationRepository.AddAsync(notification);

                await _roleRequestRepository.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "The role request could not be approved because the database state changed. Please reload and try again.",
                    ex);
            }

            return MapResponse(roleRequest);
        }

        public async Task<RoleRequestResponse> DenyAsync(
            int id,
            int adminId,
            RoleRequestDecisionRequest request)
        {
            var roleRequest = await LoadForDecisionAsync(id);

            if (roleRequest == null)
            {
                throw new KeyNotFoundException(
                    $"Role request {id} was not found.");
            }

            EnsurePending(roleRequest);
            ValidateNotes(request.Notes, required: true);

            var now = DateTime.UtcNow;

            roleRequest.Status = "DENIED";
            roleRequest.Notes = NormalizeNotes(request.Notes);
            roleRequest.ReviewedByAdminId = adminId;
            roleRequest.ReviewedAt = now;
            roleRequest.UpdatedAt = now;

            if (string.Equals(
                    roleRequest.RequestType,
                    "INITIAL_REGISTRATION",
                    StringComparison.OrdinalIgnoreCase))
            {
                roleRequest.User.IsActive = false;
                roleRequest.User.VerificationStatus = "Rejected";
                roleRequest.User.UpdatedAt = now;
            }

            var noteSuffix = !string.IsNullOrWhiteSpace(request.Notes) ? $" Lý do: {request.Notes}" : "";
            var notification = new Notification
            {
                UserId = roleRequest.UserId,
                Message = $"Yêu cầu xét duyệt vai trò \"{roleRequest.RequestedRole?.Name}\" của bạn đã bị từ chối.{noteSuffix}",
                IsRead = false,
                CreatedAt = now
            };
            await _notificationRepository.AddAsync(notification);

            await _roleRequestRepository.SaveChangesAsync();

            return MapResponse(roleRequest);
        }

        private async Task<RoleRequest?> LoadForDecisionAsync(int id)
        {
            return await _roleRequestRepository
                .GetQueryable()
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .Include(x => x.RequestedRole)
                .FirstOrDefaultAsync(x => x.RoleRequestId == id);
        }

        private static void EnsurePending(RoleRequest roleRequest)
        {
            if (!string.Equals(
                    roleRequest.Status,
                    "PENDING",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Role request {roleRequest.RoleRequestId} is already {roleRequest.Status} and can no longer be changed.");
            }
        }

        private static void ValidateNotes(
            string? notes,
            bool required)
        {
            var normalized = NormalizeNotes(notes);

            if (required
                && (normalized == null || normalized.Length < 10))
            {
                throw new ArgumentException(
                    "Denial reason must be at least 10 characters.");
            }

            if (normalized != null && normalized.Length > 1000)
            {
                throw new ArgumentException(
                    "Notes must not exceed 1000 characters.");
            }
        }

        private static string? NormalizeNotes(string? notes)
        {
            return string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();
        }

        private static RoleRequestResponse MapResponse(
            RoleRequest roleRequest)
        {
            var requestedRoleName =
                roleRequest.RequestedRole?.Name
                ?? string.Empty;

            var currentRoles = roleRequest.User.UserRoles
                .Where(x => x.Role != null)
                .Select(x => x.Role!.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var isAdditionalRole = string.Equals(
                roleRequest.RequestType,
                "ADDITIONAL_ROLE",
                StringComparison.OrdinalIgnoreCase);

            return new RoleRequestResponse
            {
                Id = roleRequest.RoleRequestId,
                UserId = roleRequest.UserId,
                UserName = roleRequest.User.FullName,
                Email = roleRequest.User.Email,
                Phone = roleRequest.PhoneNumber,
                Affiliation = roleRequest.Affiliation ?? string.Empty,
                Department = roleRequest.Department ?? string.Empty,
                CurrentRoles = currentRoles,
                RequestedAdditionalRoles =
                    isAdditionalRole && !string.IsNullOrWhiteSpace(requestedRoleName)
                        ? new List<string> { requestedRoleName }
                        : new List<string>(),
                RequestType = roleRequest.RequestType,
                RequestedRoles =
                    string.IsNullOrWhiteSpace(requestedRoleName)
                        ? new List<string>()
                        : new List<string> { requestedRoleName },
                ProofDocumentUrl = roleRequest.ProofDocumentUrl,
                IsEmailVerified = roleRequest.User.IsEmailVerified,
                SubmissionDate = roleRequest.CreatedAt,
                Status = roleRequest.Status.ToUpperInvariant(),
                Notes = roleRequest.Notes
            };
        }
    }
}
