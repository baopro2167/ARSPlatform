using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class SharedMaterialService : ISharedMaterialService
    {
        private readonly ISharedMaterialRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public SharedMaterialService(
            ISharedMaterialRepository repository,
            AppDbContext dbContext,
            IMapper mapper)
        {
            _repository = repository;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SharedMaterialResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return items.Select(x => MapToResponse(x, 0)).ToList();
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = paged.Items.Select(x => MapToResponse(x, 0)).ToList();
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = paged.Items.Select(x => MapToResponse(x, lecturerId)).ToList();
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPaperIdPagedAsync(paperId, pageNumber, pageSize);
            var dtos = paged.Items.Select(x => MapToResponse(x, 0)).ToList();
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<SharedMaterialResponse?> GetByIdAsync(int id)
        {
            return await GetByIdAsync(id, 0);
        }

        public async Task<SharedMaterialResponse?> GetByIdAsync(int id, int currentUserId)
        {
            var item = await _repository.GetWithDetailsByIdAsync(id);
            return item == null ? null : MapToResponse(item, currentUserId);
        }

        public async Task<SharedMaterialResponse> CreateAsync(SharedMaterialCreateRequest request)
        {
            var senderId = request.LecturerId ?? 0;
            return await CreateShareAsync(request, senderId, isAdmin: true);
        }

        public async Task<SharedMaterialResponse> CreateShareAsync(SharedMaterialCreateRequest request, int currentUserId, bool isAdmin = false)
        {
            var materialId = request.LearningMaterialId ?? request.PaperId;
            if (!materialId.HasValue || materialId.Value <= 0)
            {
                throw new ArgumentException("A valid numeric learningMaterialId (or paperId) is required.");
            }

            if (!request.SharedWithColleagueId.HasValue || request.SharedWithColleagueId.Value <= 0)
            {
                throw new ArgumentException("sharedWithColleagueId is required.");
            }

            var colleagueId = request.SharedWithColleagueId.Value;
            var senderId = (request.LecturerId.HasValue && request.LecturerId.Value > 0)
                ? (isAdmin ? request.LecturerId.Value : currentUserId)
                : currentUserId;

            if (senderId == colleagueId)
            {
                throw new ArgumentException("You cannot share learning materials with yourself.");
            }

            var colleague = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == colleagueId);
            if (colleague == null || colleague.IsActive == false)
            {
                throw new KeyNotFoundException($"Colleague with ID {colleagueId} was not found or is inactive.");
            }

            var learningMaterial = await _dbContext.LearningMaterials.AsNoTracking()
                .FirstOrDefaultAsync(m => m.LearningMaterialId == materialId.Value);

            int? resolvedLearningMaterialId = null;
            int? resolvedPaperId = null;

            if (learningMaterial != null)
            {
                if (!isAdmin && learningMaterial.LecturerId.HasValue && learningMaterial.LecturerId.Value != senderId)
                {
                    throw new UnauthorizedAccessException("You do not own this learning material.");
                }
                resolvedLearningMaterialId = learningMaterial.LearningMaterialId;
            }
            else
            {
                var paper = await _dbContext.Papers.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PaperId == materialId.Value);
                if (paper != null)
                {
                    if (!isAdmin && paper.CreatorId.HasValue && paper.CreatorId.Value != senderId)
                    {
                        throw new UnauthorizedAccessException("You do not own this paper.");
                    }
                    resolvedPaperId = paper.PaperId;
                }
                else
                {
                    throw new KeyNotFoundException($"Material with ID {materialId.Value} was not found.");
                }
            }

            var duplicate = await _repository.FindPendingDuplicateAsync(senderId, colleagueId, resolvedLearningMaterialId, resolvedPaperId);
            if (duplicate != null)
            {
                throw new InvalidOperationException("A pending share invitation for this material and colleague already exists.");
            }

            var now = DateTime.UtcNow;
            var sharedAt = request.SharedAt ?? now;
            var expiresAt = request.ExpiresAt ?? sharedAt.AddDays(30);

            var entity = new SharedMaterial
            {
                LecturerId = senderId,
                LearningMaterialId = resolvedLearningMaterialId,
                PaperId = resolvedPaperId,
                SharedWithColleagueId = colleagueId,
                SharedAt = sharedAt,
                ExpiresAt = expiresAt,
                Status = "PENDING",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var loaded = await _repository.GetWithDetailsByIdAsync(entity.SharedMaterialId);
            return MapToResponse(loaded ?? entity, currentUserId);
        }

        public async Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request)
        {
            var senderId = request.LecturerId ?? 0;
            return await UpdateAsync(id, request, senderId, isAdmin: true);
        }

        public async Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request, int currentUserId, bool isAdmin = false)
        {
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                return await UpdateStatusAsync(id, request.Status, currentUserId, request.RespondedAt, isAdmin);
            }

            var item = await _repository.GetWithDetailsByIdAsync(id);
            if (item == null) return null;

            if (request.SharedWithColleagueId.HasValue) item.SharedWithColleagueId = request.SharedWithColleagueId.Value;
            if (request.LearningMaterialId.HasValue) item.LearningMaterialId = request.LearningMaterialId.Value;
            if (request.PaperId.HasValue) item.PaperId = request.PaperId.Value;
            if (request.SharedAt.HasValue) item.SharedAt = request.SharedAt.Value;
            if (request.ExpiresAt.HasValue) item.ExpiresAt = request.ExpiresAt.Value;
            if (request.RespondedAt.HasValue) item.RespondedAt = request.RespondedAt.Value;
            item.UpdatedAt = DateTime.UtcNow;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            return MapToResponse(item, currentUserId);
        }

        public async Task<SharedMaterialResponse?> UpdateStatusAsync(int id, string newStatus, int currentUserId, DateTime? respondedAt = null, bool isAdmin = false)
        {
            var item = await _repository.GetWithDetailsByIdAsync(id);
            if (item == null) return null;

            var now = DateTime.UtcNow;
            var expiresAt = item.ExpiresAt ?? (item.SharedAt.HasValue ? item.SharedAt.Value.AddDays(30) : now.AddDays(30));
            if (now > expiresAt)
            {
                throw new InvalidOperationException("This share invitation has expired.");
            }

            var normalized = newStatus.Trim().ToUpperInvariant();
            var isSender = item.LecturerId == currentUserId;
            var isRecipient = item.SharedWithColleagueId == currentUserId;

            if (normalized is "ACCEPTED" or "DECLINED")
            {
                if (!isRecipient && !isAdmin)
                {
                    throw new UnauthorizedAccessException("Only the recipient can accept or decline this share invitation.");
                }
            }
            else if (normalized is "REVOKED" or "CANCELLED")
            {
                if (!isSender && !isAdmin)
                {
                    throw new UnauthorizedAccessException("Only the sender can revoke this share invitation.");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid status: '{newStatus}'. Allowed: ACCEPTED, DECLINED, REVOKED.");
            }

            item.Status = normalized;
            item.RespondedAt = respondedAt ?? now;
            item.UpdatedAt = now;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            return MapToResponse(item, currentUserId);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeOrDeleteAsync(int id, int currentUserId, bool isAdmin = false)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            var isSender = item.LecturerId == currentUserId;
            if (!isSender && !isAdmin)
            {
                throw new UnauthorizedAccessException("Only the sender can cancel or delete this share.");
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<List<SharedMaterialResponse>> GetFeedAsync(int currentUserId, bool includeExpired = false, string? status = null, int? learningMaterialId = null)
        {
            var list = await _repository.GetFeedAsync(currentUserId, includeExpired, status, learningMaterialId);
            return list.Select(x => MapToResponse(x, currentUserId)).ToList();
        }

        private static SharedMaterialResponse MapToResponse(SharedMaterial item, int currentUserId)
        {
            var now = DateTime.UtcNow;
            var expiresAt = item.ExpiresAt ?? (item.SharedAt.HasValue ? item.SharedAt.Value.AddDays(30) : now.AddDays(30));
            var isExpired = now > expiresAt;
            var rawStatus = item.Status?.Trim().ToUpperInvariant() ?? "PENDING";
            var effectiveStatus = isExpired ? "EXPIRED" : rawStatus;

            var isSender = item.LecturerId == currentUserId;
            var isRecipient = item.SharedWithColleagueId == currentUserId;
            var direction = isSender ? "outbound" : "inbound";

            var canRevoke = isSender && !isExpired && (rawStatus == "PENDING" || rawStatus == "Pending");
            var canRespond = isRecipient && !isExpired && (rawStatus == "PENDING" || rawStatus == "Pending");

            var daysRemaining = isExpired ? 0 : Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalDays));

            var learningMaterialId = item.LearningMaterialId ?? item.PaperId;
            var materialTitle = item.LearningMaterial?.Title ?? item.Paper?.Title ?? $"Material #{learningMaterialId}";
            var materialUrl = item.LearningMaterial?.FileUrl ?? item.Paper?.FileUrl;
            var description = item.LearningMaterial?.Description ?? item.Paper?.Abstract;

            return new SharedMaterialResponse
            {
                SharedMaterialId = item.SharedMaterialId,
                Direction = direction,
                LecturerId = item.LecturerId,
                LecturerName = item.Lecturer?.FullName ?? (item.LecturerId.HasValue ? $"Dr. #{item.LecturerId}" : null),
                SharedWithColleagueId = item.SharedWithColleagueId,
                SharedWithName = item.SharedWithColleague?.FullName ?? (item.SharedWithColleagueId.HasValue ? $"Dr. #{item.SharedWithColleagueId}" : null),
                LearningMaterialId = learningMaterialId,
                LearningMaterialTitle = materialTitle,
                LearningMaterialUrl = materialUrl,
                Title = materialTitle,
                FileUrl = materialUrl,
                Url = materialUrl,
                Description = description,
                PaperId = learningMaterialId,
                SharedAt = item.SharedAt,
                ExpiresAt = expiresAt,
                RespondedAt = item.RespondedAt,
                Status = rawStatus,
                EffectiveStatus = effectiveStatus,
                CanRevoke = canRevoke,
                CanRespond = canRespond,
                DaysRemaining = daysRemaining,
                CreatedAt = item.CreatedAt ?? item.SharedAt,
                UpdatedAt = item.UpdatedAt ?? item.SharedAt
            };
        }
    }
}
