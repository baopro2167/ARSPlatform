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
        private readonly IResearchTopicRepository _topicRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public PhasedReportService(
            IPhasedReportRepository repository,
            IResearchGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IResearchTopicRepository topicRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _topicRepository = topicRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PhasedReportResponse>> GetAllAsync(int? researchGroupId = null, int? topicId = null)
        {
            Expression<Func<PhasedReport, bool>>? predicate = null;
            if (researchGroupId.HasValue && topicId.HasValue)
            {
                predicate = x => x.ResearchGroupId == researchGroupId.Value && x.TopicId == topicId.Value;
            }
            else if (researchGroupId.HasValue)
            {
                predicate = x => x.ResearchGroupId == researchGroupId.Value;
            }
            else if (topicId.HasValue)
            {
                predicate = x => x.TopicId == topicId.Value;
            }

            var items = await _repository.GetAllAsync(predicate, includes: new Expression<Func<PhasedReport, object>>[]
            {
                x => x.ResearchGroup!,
                x => x.GroupMember!,
                x => x.GroupMember!.Student!,
                x => x.Topic!
            });

            return _mapper.Map<IEnumerable<PhasedReportResponse>>(items.OrderBy(x => x.PhaseNumber));
        }

        public async Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams, int? researchGroupId = null, int? topicId = null)
        {
            Expression<Func<PhasedReport, bool>>? predicate = null;
            if (researchGroupId.HasValue && topicId.HasValue)
            {
                predicate = x => x.ResearchGroupId == researchGroupId.Value && x.TopicId == topicId.Value;
            }
            else if (researchGroupId.HasValue)
            {
                predicate = x => x.ResearchGroupId == researchGroupId.Value;
            }
            else if (topicId.HasValue)
            {
                predicate = x => x.TopicId == topicId.Value;
            }

            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderBy(x => x.PhaseNumber),
                includes: new Expression<Func<PhasedReport, object>>[]
                {
                    x => x.ResearchGroup!,
                    x => x.GroupMember!,
                    x => x.GroupMember!.Student!,
                    x => x.Topic!
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
                x => x.GroupMember!.Student!,
                x => x.Topic!)).FirstOrDefault();
            return item == null ? null : _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<IEnumerable<PhasedReportResponse>> GetByTopicIdAsync(int topicId)
        {
            return await GetAllAsync(topicId: topicId);
        }

        public async Task<IEnumerable<GroupMemberResponse>> GetMembersByTopicIdAsync(int topicId)
        {
            // Lấy tất cả nhóm nghiên cứu thuộc Topic
            var groups = await _groupRepository.GetAllAsync(x => x.TopicId == topicId);
            var groupIds = groups.Select(g => g.ResearchGroupId).ToList();

            if (!groupIds.Any())
            {
                return Enumerable.Empty<GroupMemberResponse>();
            }

            var members = await _groupMemberRepository.GetAllAsync(
                x => x.ResearchGroupId.HasValue && groupIds.Contains(x.ResearchGroupId.Value),
                includes: x => x.Student!);

            return _mapper.Map<IEnumerable<GroupMemberResponse>>(members);
        }

        public async Task<IEnumerable<PhasedReportResponse>> CreateTopicMilestonesAsync(TopicMilestonesCreateRequest request, int? lecturerUserId = null)
        {
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đề tài nghiên cứu (ResearchTopic).");
            }

            var existingPhases = (await _repository.GetAllAsync(x => x.TopicId == request.TopicId)).ToList();
            var resultList = new List<PhasedReport>();

            foreach (var phaseItem in request.Phases)
            {
                var existing = existingPhases.FirstOrDefault(p => p.PhaseNumber == phaseItem.PhaseNumber);
                if (existing != null)
                {
                    existing.MilestoneTitle = phaseItem.MilestoneTitle;
                    existing.DeadlineAt = phaseItem.DeadlineAt;
                    existing.Requirements = phaseItem.Requirements ?? existing.Requirements;
                    existing.AssessmentCriteria = phaseItem.AssessmentCriteria ?? existing.AssessmentCriteria;
                    existing.StartDate = phaseItem.StartDate ?? existing.StartDate;
                    existing.ResearchGroupId = request.ResearchGroupId ?? existing.ResearchGroupId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _repository.Update(existing);
                    resultList.Add(existing);
                }
                else
                {
                    var newPhase = new PhasedReport
                    {
                        TopicId = request.TopicId,
                        ResearchGroupId = request.ResearchGroupId,
                        PhaseNumber = phaseItem.PhaseNumber,
                        MilestoneTitle = phaseItem.MilestoneTitle,
                        Requirements = phaseItem.Requirements,
                        AssessmentCriteria = phaseItem.AssessmentCriteria,
                        StartDate = phaseItem.StartDate,
                        DeadlineAt = phaseItem.DeadlineAt,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _repository.AddAsync(newPhase);
                    resultList.Add(newPhase);
                }
            }

            await _repository.SaveChangesAsync();
            return await GetByTopicIdAsync(request.TopicId);
        }

        public async Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request, int? currentUserId = null)
        {
            var item = _mapper.Map<PhasedReport>(request);

            if (string.IsNullOrWhiteSpace(item.Status))
            {
                item.Status = "Pending";
            }

            item.CreatedAt ??= DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            // Nếu nộp bài ngay lúc tạo
            if (!string.IsNullOrWhiteSpace(item.ReportFileUrl))
            {
                item.SubmittedAt ??= DateTime.UtcNow;
                if (item.DeadlineAt.HasValue && item.SubmittedAt.Value > item.DeadlineAt.Value)
                {
                    item.Status = "Overdue";
                }
                else
                {
                    item.Status = "OnTime";
                }
            }

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
            if (item.ResearchGroupId.HasValue || item.TopicId.HasValue)
            {
                try
                {
                    int? lecturerId = null;
                    string groupOrTopicName = "";

                    if (item.ResearchGroupId.HasValue)
                    {
                        var group = await _groupRepository.GetByIdAsync(item.ResearchGroupId.Value);
                        lecturerId = group?.LecturerId;
                        groupOrTopicName = group?.Name ?? "";
                    }

                    if (!lecturerId.HasValue && item.TopicId.HasValue)
                    {
                        var topic = await _topicRepository.GetByIdAsync(item.TopicId.Value);
                        lecturerId = topic?.LecturerId;
                        groupOrTopicName = topic?.Title ?? "";
                    }

                    if (lecturerId.HasValue)
                    {
                        var phase = item.PhaseNumber ?? 1;
                        var notif = new Notification
                        {
                            UserId = lecturerId.Value,
                            Message = $"[Báo cáo tiến độ] Đã tạo/nộp báo cáo Phase {phase}: \"{groupOrTopicName}\"",
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
                item.Status = request.LectureFeedback.Value >= 5.0m ? "Passed" : "Rejected";
            }

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Thông báo cho sinh viên khi giảng viên chấm điểm
            if (item.GroupMember?.StudentId != null && (request.LectureFeedback.HasValue || !string.IsNullOrWhiteSpace(request.CapacityEvaluation) || !string.IsNullOrWhiteSpace(request.LecturerDescription)))
            {
                try
                {
                    var phase = item.PhaseNumber ?? 1;
                    var scoreText = item.LectureFeedback.HasValue ? $" ({item.LectureFeedback.Value}/10)" : string.Empty;
                    var notif = new Notification
                    {
                        UserId = item.GroupMember.StudentId.Value,
                        Message = $"[Báo cáo tiến độ] Giảng viên đã chấm và nhận xét báo cáo Phase {phase}{scoreText} - Trạng thái: {item.Status ?? "Evaluated"}",
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

        public async Task<PhasedReportResponse> SubmitReportAsync(PhasedReportSubmitRequest request, int? currentUserId = null)
        {
            PhasedReport? item = null;

            if (request.PhasedReportId.HasValue)
            {
                item = (await _repository.GetAllAsync(x => x.PhasedReportId == request.PhasedReportId.Value,
                    x => x.ResearchGroup!,
                    x => x.Topic!)).FirstOrDefault();
            }
            else if (request.TopicId.HasValue && request.PhaseNumber.HasValue)
            {
                item = (await _repository.GetAllAsync(x => x.TopicId == request.TopicId.Value && x.PhaseNumber == request.PhaseNumber.Value,
                    x => x.ResearchGroup!,
                    x => x.Topic!)).FirstOrDefault();
            }

            if (item == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giai đoạn báo cáo (PhasedReport) cần nộp bài.");
            }

            // Kiểm tra ResearchGroupId và ResearchTopic
            var targetGroupId = request.ResearchGroupId ?? item.ResearchGroupId;
            if (targetGroupId.HasValue)
            {
                var group = await _groupRepository.GetByIdAsync(targetGroupId.Value);
                if (group == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy nhóm nghiên cứu (ResearchGroup).");
                }

                if (item.TopicId.HasValue && group.TopicId.HasValue && group.TopicId.Value != item.TopicId.Value)
                {
                    throw new InvalidOperationException($"Nhóm nghiên cứu \"{group.Name}\" không thuộc đề tài nghiên cứu của cột mốc này.");
                }

                item.ResearchGroupId = targetGroupId;
            }

            // Tự động tìm GroupMemberId nếu chưa truyền
            if (!request.GroupMemberId.HasValue && item.ResearchGroupId.HasValue && currentUserId.HasValue)
            {
                var gm = (await _groupMemberRepository.GetAllAsync(x => x.ResearchGroupId == item.ResearchGroupId.Value && x.StudentId == currentUserId.Value)).FirstOrDefault();
                if (gm != null)
                {
                    item.GroupMemberId = gm.GroupMemberId;
                }
            }
            else if (request.GroupMemberId.HasValue)
            {
                item.GroupMemberId = request.GroupMemberId.Value;
            }

            // Cập nhật thời gian nộp và so sánh Deadline
            var nowUtc = DateTime.UtcNow;
            item.SubmittedAt = nowUtc;
            item.UpdatedAt = nowUtc;
            item.ReportFileUrl = request.ReportFileUrl;

            if (item.DeadlineAt.HasValue && nowUtc > item.DeadlineAt.Value)
            {
                item.Status = "Overdue";
            }
            else
            {
                item.Status = "OnTime";
            }

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Gửi thông báo cho Giảng viên
            try
            {
                int? lecturerId = null;
                if (item.ResearchGroupId.HasValue)
                {
                    var group = await _groupRepository.GetByIdAsync(item.ResearchGroupId.Value);
                    lecturerId = group?.LecturerId;
                }
                if (!lecturerId.HasValue && item.TopicId.HasValue)
                {
                    var topic = await _topicRepository.GetByIdAsync(item.TopicId.Value);
                    lecturerId = topic?.LecturerId;
                }

                if (lecturerId.HasValue)
                {
                    var statusText = item.Status == "Overdue" ? "Nộp muộn (Overdue)" : "Đúng hạn (OnTime)";
                    var notif = new Notification
                    {
                        UserId = lecturerId.Value,
                        Message = $"[Báo cáo tiến độ] Trưởng nhóm đã nộp bài Phase {item.PhaseNumber ?? 1} ({statusText})",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notif);
                    await _notificationRepository.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore notification error
            }

            var updated = await GetByIdAsync(item.PhasedReportId);
            return updated ?? _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<PhasedReportResponse?> EvaluateReportAsync(int phasedReportId, PhasedReportEvaluationRequest request, int? lecturerUserId = null)
        {
            var item = (await _repository.GetAllAsync(x => x.PhasedReportId == phasedReportId,
                x => x.ResearchGroup!,
                x => x.GroupMember!)).FirstOrDefault();
            if (item == null) return null;

            if (!string.IsNullOrWhiteSpace(request.LecturerDescription))
                item.LecturerDescription = request.LecturerDescription;

            if (request.LectureFeedback.HasValue)
                item.LectureFeedback = request.LectureFeedback;

            if (!string.IsNullOrWhiteSpace(request.CapacityEvaluation))
                item.CapacityEvaluation = request.CapacityEvaluation;

            if (!string.IsNullOrWhiteSpace(request.FinalOutcomeEvaluation))
                item.FinalOutcomeEvaluation = request.FinalOutcomeEvaluation;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                item.Status = request.Status;
            }
            else if (request.LectureFeedback.HasValue)
            {
                item.Status = request.LectureFeedback.Value >= 5.0m ? "Passed" : "Rejected";
            }
            else
            {
                item.Status = "Evaluated";
            }

            item.UpdatedAt = DateTime.UtcNow;
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Gửi thông báo cho sinh viên/nhóm
            if (item.GroupMember?.StudentId != null)
            {
                try
                {
                    var phase = item.PhaseNumber ?? 1;
                    var scoreText = item.LectureFeedback.HasValue ? $" ({item.LectureFeedback.Value}/10)" : string.Empty;
                    var notif = new Notification
                    {
                        UserId = item.GroupMember.StudentId.Value,
                        Message = $"[Báo cáo tiến độ] Giảng viên đã nhận xét và chấm điểm Phase {phase}{scoreText} - Trạng thái: {item.Status}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notif);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Ignore notification error
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
