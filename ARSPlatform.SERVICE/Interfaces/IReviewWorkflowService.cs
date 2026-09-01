using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IReviewWorkflowService
    {
        // === DÀNH CHO REVIEWER ===
        Task<IEnumerable<ReviewerAssignmentResponse>> GetAssignmentsForReviewerAsync(int reviewerId);
        Task<ReviewerAssignmentResponse?> GetAssignmentByIdAsync(int assignmentId, int reviewerId);
        Task<ReviewerAssignmentResponse> AcceptAssignmentAsync(int assignmentId, int reviewerId);
        Task<ReviewerAssignmentResponse> DeclineAssignmentAsync(int assignmentId, int reviewerId, ReviewerDeclineRequest request);
        Task<ReviewerAssignmentResponse> DeclareConflictOfInterestAsync(int assignmentId, int reviewerId, ReviewerConflictOfInterestRequest request);
        Task<PaperReviewResponse> SubmitReviewAsync(int assignmentId, int reviewerId, PaperReviewSubmitRequest request);

        // === DÀNH CHO ADMIN ===
        Task<AdminPaperReviewsSummaryResponse> GetPaperReviewsSummaryForAdminAsync(int paperId);
        Task<AdminPaperReviewsSummaryResponse> AdminPublishPaperAsync(int paperId, int adminId, AdminPublishPaperRequest request);
        Task<AdminPaperReviewsSummaryResponse> AdminRequestRevisionAsync(int paperId, int adminId, AdminRequestRevisionRequest request);
        Task<AdminPaperReviewsSummaryResponse> AdminRejectPaperAsync(int paperId, int adminId, AdminRejectPaperRequest request);

        // === DÀNH CHO RESEARCHER (TÁC GIẢ) ===
        Task<AuthorPaperReviewFeedbackResponse> GetAuthorPaperReviewsAsync(int paperId, int researcherId);
    }
}
