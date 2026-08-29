using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class PhasedReportService : IPhasedReportService
    {
        private readonly IPhasedReportRepository _repository;
        private readonly IResearchGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public PhasedReportService(
            IPhasedReportRepository repository,
            IResearchGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PhasedReportResponse>> GetAllAsync(int? researchGroupId = null)
        {
            Expression<Func<PhasedReport, bool>>? predicate = researchGroupId.HasValue
                ? x => x.ResearchGroupId == researchGroupId.Value
                : null;

            var items = await _repository.GetAllAsync(predicate, includes: new Expression<Func<PhasedReport, object>>[]
            {
                x => x.ResearchGroup!,
                x => x.GroupMember!,
                x => x.GroupMember!.Student!
            });
            return _mapper.Map<IEnumerable<PhasedReportResponse>>(items);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams, int? researchGroupId = null)
        {
            Expression<Func<PhasedReport, bool>>? predicate = researchGroupId.HasValue
                ? x => x.ResearchGroupId == researchGroupId.Value
                : null;

            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.SubmittedAt),
                includes: new Expression<Func<PhasedReport, object>>[]
                {
                    x => x.ResearchGroup!,
                    x => x.GroupMember!,
                    x => x.GroupMember!.Student!
                });
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetByResearchGroupIdAsync(int researchGroupId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByResearchGroupIdPagedAsync(researchGroupId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetByGroupMemberIdAsync(int groupMemberId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByGroupMemberIdPagedAsync(groupMemberId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PhasedReportResponse?> GetByIdAsync(int id)
        {
            var item = (await _repository.GetAllAsync(x => x.PhasedReportId == id,
                x => x.ResearchGroup!,
                x => x.GroupMember!,
                x => x.GroupMember!.Student!)).FirstOrDefault();
            return item == null ? null : _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request, int? currentUserId = null)
        {
            var item = _mapper.Map<PhasedReport>(request);

            if (string.IsNullOrWhiteSpace(item.Status))
            {
                item.Status = "SUBMITTED";
            }

            if (!item.SubmittedAt.HasValue)
            {
                item.SubmittedAt = DateTime.UtcNow;
            }

            item.UpdatedAt = DateTime.UtcNow;

            // Nếu GroupMemberId chưa được truyền, tìm kiếm theo researchGroupId và currentUserId
            if (!item.GroupMemberId.HasValue && item.ResearchGroupId.HasValue && currentUserId.HasValue)
            {
                var gm = (await _groupMemberRepository.GetAllAsync(x => x.ResearchGroupId == item.ResearchGroupId.Value && x.StudentId == currentUserId.Value)).FirstOrDefault();
                if (gm != null)
                {
                    item.GroupMemberId = gm.GroupMemberId;
                }
            }

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            // Thông báo cho Giảng viên hướng dẫn
            if (item.ResearchGroupId.HasValue)
            {
                try
                {
                    var group = await _groupRepository.GetByIdAsync(item.ResearchGroupId.Value);
                    if (group?.LecturerId != null)
                    {
                        var phase = item.PhaseNumber ?? 1;
                        var notif = new Notification
                        {
                            UserId = group.LecturerId.Value,
                            Message = $"[Báo cáo tiến độ] Sinh viên đã nộp báo cáo Phase {phase}: \"{group.Name}\"",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationRepository.AddAsync(notif);
                        await _notificationRepository.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Ignore notification errors
                }
            }

            var created = await GetByIdAsync(item.PhasedReportId);
            return created ?? _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<PhasedReportResponse?> UpdateAsync(int id, PhasedReportUpdateRequest request, int? currentUserId = null)
        {
            var item = (await _repository.GetAllAsync(x => x.PhasedReportId == id,
                x => x.ResearchGroup!,
                x => x.GroupMember!)).FirstOrDefault();
            if (item == null) return null;

            _mapper.Map(request, item);
            item.UpdatedAt = DateTime.UtcNow;

            // Nếu giảng viên chấm điểm mà chưa truyền status cụ thể
            if (request.LectureFeedback.HasValue && string.IsNullOrWhiteSpace(request.Status))
            {
                item.Status = request.LectureFeedback.Value >= 5.0m ? "EVALUATED" : "REJECTED";
            }

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Thông báo cho sinh viên khi giảng viên chấm điểm
            if (item.GroupMember?.StudentId != null && (request.LectureFeedback.HasValue || !string.IsNullOrWhiteSpace(request.CapacityEvaluation) || !string.IsNullOrWhiteSpace(request.Status)))
            {
                try
                {
                    var phase = item.PhaseNumber ?? 1;
                    var scoreText = item.LectureFeedback.HasValue ? $" ({item.LectureFeedback.Value}/10)" : string.Empty;
                    var notif = new Notification
                    {
                        UserId = item.GroupMember.StudentId.Value,
                        Message = $"[Báo cáo tiến độ] Giảng viên đã chấm báo cáo Phase {phase}{scoreText} - Trạng thái: {item.Status ?? "EVALUATED"}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notif);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Ignore notification errors
                }
            }

            var updated = await GetByIdAsync(item.PhasedReportId);
            return updated ?? _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
