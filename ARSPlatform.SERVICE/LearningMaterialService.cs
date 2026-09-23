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
using ARSPlatform.REPOSITORIES;

namespace ARSPlatform.SERVICES
{
    public class LearningMaterialService : ILearningMaterialService
    {
        private readonly ILearningMaterialRepository _repository;
        private readonly IResearchTopicRepository _researchTopicRepository;
        private readonly IResearchTopicLearningMaterialRepository _topicMaterialRepository;
        private readonly IPhasedReportRepository _phasedReportRepository;
        private readonly ISharedMaterialRepository _sharedMaterialRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMapper _mapper;

        public LearningMaterialService(
            ILearningMaterialRepository repository,
            IResearchTopicRepository researchTopicRepository,
            IResearchTopicLearningMaterialRepository topicMaterialRepository,
            IPhasedReportRepository phasedReportRepository,
            ISharedMaterialRepository sharedMaterialRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IMapper mapper)
        {
            _repository = repository;
            _researchTopicRepository = researchTopicRepository;
            _topicMaterialRepository = topicMaterialRepository;
            _phasedReportRepository = phasedReportRepository;
            _sharedMaterialRepository = sharedMaterialRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _dbContextFactory = dbContextFactory;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LearningMaterialResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<LearningMaterialResponse>>(items);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetBySubFieldIdAsync(int subFieldId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetBySubFieldIdPagedAsync(subFieldId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<LearningMaterialResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<LearningMaterialResponse>(item);
        }

        public async Task<LearningMaterialResponse> CreateAsync(LearningMaterialCreateRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.FileUrl))
            {
                if (!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var uriResult) ||
                    !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("FileUrl must be a valid http or https URL.");
                }
            }

            if (request.TopicId.HasValue)
            {
                var topic = await _researchTopicRepository.GetByIdAsync(request.TopicId.Value);
                if (topic == null)
                    throw new KeyNotFoundException($"Research topic with ID {request.TopicId.Value} not found.");

                if (request.LecturerId.HasValue && topic.LecturerId != request.LecturerId.Value)
                    throw new UnauthorizedAccessException("You are not authorized to add materials to this research topic.");

                // Dùng IDbContextFactory CHỈ cho transaction
                await using var ctx = await _dbContextFactory.CreateDbContextAsync();
                var strategy = ctx.Database.CreateExecutionStrategy();
                LearningMaterial? createdMaterial = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await ctx.Database.BeginTransactionAsync();
                    try
                    {
                        var localMaterialRepo = new LearningMaterialRepository(ctx);
                        var localTopicMaterialRepo = new ResearchTopicLearningMaterialRepository(ctx);

                        var item = _mapper.Map<LearningMaterial>(request);
                        item.CreatedAt = DateTime.UtcNow;
                        await localMaterialRepo.AddAsync(item);
                        await localMaterialRepo.SaveChangesAsync();

                        var link = new ResearchTopicLearningMaterial
                        {
                            TopicId = request.TopicId.Value,
                            LearningMaterialId = item.LearningMaterialId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await localTopicMaterialRepo.AddAsync(link);
                        await localTopicMaterialRepo.SaveChangesAsync();

                        await tx.CommitAsync();
                        createdMaterial = item;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                });

                return _mapper.Map<LearningMaterialResponse>(createdMaterial);
            }
            else
            {
                var item = _mapper.Map<LearningMaterial>(request);
                item.CreatedAt = DateTime.UtcNow;
                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();
                return _mapper.Map<LearningMaterialResponse>(item);
            }
        }

        public async Task<LearningMaterialResponse?> UpdateAsync(int id, LearningMaterialUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<LearningMaterialResponse>(item);
        }

        public async Task<(bool Success, int RevokedSharesCount, string Message)> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return (false, 0, "Learning material not found.");

            // 1. Kiểm tra tài liệu có đang được dùng trong ResearchTopic/PhasedReport không
            var isUsedInTopic = await _researchTopicRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);
            if (isUsedInTopic)
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");

            var isUsedInPhasedReport = await _phasedReportRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);
            if (isUsedInPhasedReport)
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");

            // 2. Cascade: lấy shares liên quan, gửi notification, xóa shares
            var relatedShares = (await _sharedMaterialRepository.GetByLearningMaterialIdAsync(id)).ToList();
            var revokedSharesCount = relatedShares.Count;

            if (revokedSharesCount > 0)
            {
                var senderUser = await _userRepository.GetByIdAsync(item.LecturerId ?? 0);
                var senderName = senderUser?.FullName ?? item.Lecturer?.FullName ?? "Giảng viên chủ sở hữu";
                var now = DateTime.UtcNow;

                foreach (var s in relatedShares)
                {
                    if (s.SharedWithColleagueId.HasValue)
                    {
                        var notif = new Notification
                        {
                            UserId = s.SharedWithColleagueId.Value,
                            Message = $"Tài liệu \"{item.Title}\" do Giảng viên {senderName} chia sẻ đã bị chủ sở hữu xóa khỏi hệ thống.",
                            IsRead = false,
                            CreatedAt = now
                        };
                        await _notificationRepository.AddAsync(notif);
                    }
                    _sharedMaterialRepository.Delete(s);
                }

                await _notificationRepository.SaveChangesAsync();
            }

            // 3. Xóa tài liệu gốc
            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            var message = revokedSharesCount > 0
                ? $"Xóa tài liệu thành công. Đã thu hồi liên kết chia sẻ tới {revokedSharesCount} giảng viên và gửi thông báo tới họ."
                : "Xóa tài liệu thành công.";

            return (true, revokedSharesCount, message);
        }
    }
}
