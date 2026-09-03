using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPhasedReportService
    {
        Task<IEnumerable<PhasedReportResponse>> GetAllAsync(int? researchGroupId = null, int? topicId = null);
        Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams, int? researchGroupId = null, int? topicId = null);
        Task<PagedResult<PhasedReportResponse>> GetByResearchGroupIdAsync(int researchGroupId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetByGroupMemberIdAsync(int groupMemberId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PhasedReportResponse?> GetByIdAsync(int id);
        Task<IEnumerable<PhasedReportResponse>> GetByTopicIdAsync(int topicId);
        Task<IEnumerable<GroupMemberResponse>> GetMembersByTopicIdAsync(int topicId);
        Task<IEnumerable<PhasedReportResponse>> CreateTopicMilestonesAsync(TopicMilestonesCreateRequest request, int? lecturerUserId = null);
        Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request, int? currentUserId = null);
        Task<PhasedReportResponse?> UpdateAsync(int id, PhasedReportUpdateRequest request, int? currentUserId = null);

        /// <summary>
        /// Giảng viên (Lecture) gia hạn deadline để sinh viên nộp bài.
        /// Tự động set <c>Status = "Pending"</c> cho báo cáo.
        /// </summary>
        Task<PhasedReportResponse?> ExtendDeadlineAsync(int id, PhasedReportExtendDeadlineRequest request, int? currentUserId = null);

        Task<PhasedReportResponse> SubmitReportAsync(PhasedReportSubmitRequest request, int? currentUserId = null);
        Task<PhasedReportResponse?> EvaluateReportAsync(int phasedReportId, PhasedReportEvaluationRequest request, int? lecturerUserId = null);
        Task<bool> DeleteAsync(int id);
    }
}
