using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ReviewWorkflowService : IReviewWorkflowService
    {
        private readonly AppDbContext _context;
        private readonly INotificationRepository _notificationRepository;

        public static readonly List<(string Code, string Name, string Description)> DefaultCriteria = new()
        {
            ("SCOPE_RELEVANCE", "Phạm vi & Mức độ phù hợp", "Bài báo có phù hợp với phạm vi và lĩnh vực nghiên cứu của ARS không?"),
            ("ORIGINALITY", "Tính độc bản & Mới mẻ", "Bài báo có đóng góp mới hoặc góc nhìn độc đáo, có giá trị không?"),
            ("OBJECTIVES", "Mục tiêu & Câu hỏi nghiên cứu", "Mục tiêu và câu hỏi nghiên cứu có rõ ràng và hợp lý không?"),
            ("METHODOLOGY", "Phương pháp nghiên cứu", "Phương pháp có phù hợp, giải thích đầy đủ và đáng tin cậy không?"),
            ("EVIDENCE_RESULTS", "Bằng chứng & Kết quả", "Dữ liệu, bằng chứng, phân tích và kết luận có được hỗ trợ đầy đủ không?"),
            ("ACADEMIC_QUALITY", "Chất lượng học thuật", "Nội dung có chính xác, chuẩn mực và phù hợp với tiêu chuẩn ngành không?"),
            ("CLARITY_STRUCTURE", "Bố cục & Văn phong", "Bài viết có rõ ràng, mạch lạc, cấu trúc hợp lý và hoàn chỉnh không?"),
            ("CITATIONS_ATTRIBUTION", "Trích dẫn & Tác quyền", "Tài liệu tham khảo, trích dẫn nguồn và quyền tác giả có chuẩn xác không?"),
            ("ETHICS_COPYRIGHT", "Đạo đức & Bản quyền", "Có vấn đề về đạo đức, đạo văn, bảo mật, đồng thuận hay bản quyền không?"),
            ("METADATA_COMPLETENESS", "Tính đầy đủ của Metadata", "Tiêu đề, tác giả, đơn vị công tác, mã định danh và phân loại có đầy đủ không?"),
            ("PUBLICATION_SUITABILITY", "Khả năng xuất bản trên ARS", "Bài báo có phù hợp để xuất bản chính thức trên nền tảng ARS không?")
        };

        public ReviewWorkflowService(
            AppDbContext context,
            INotificationRepository notificationRepository)
        {
            _context = context;
            _notificationRepository = notificationRepository;
        }

        // =========================================================================
        // 1. DÀNH CHO REVIEWER (PHẢN BIỆN VIÊN)
        // =========================================================================

        public async Task<IEnumerable<ReviewerAssignmentResponse>> GetAssignmentsForReviewerAsync(int reviewerId)
        {
            var requests = await _context.ReviewRequests
                .Include(r => r.Paper)
                    .ThenInclude(p => p!.SubField)
                .Include(r => r.Reviewer)
                .Include(r => r.DetailedEvaluation)
                .Where(r => r.ReviewerId == reviewerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(r => MapToAssignmentResponse(r, includePrivateAdminComments: false));
        }

        public async Task<ReviewerAssignmentResponse?> GetAssignmentByIdAsync(int assignmentId, int reviewerId)
        {
            var request = await _context.ReviewRequests
                .Include(r => r.Paper)
                    .ThenInclude(p => p!.SubField)
                .Include(r => r.Reviewer)
                .Include(r => r.DetailedEvaluation)
                .FirstOrDefaultAsync(r => r.ReviewRequestId == assignmentId && r.ReviewerId == reviewerId);

            return request == null ? null : MapToAssignmentResponse(request, includePrivateAdminComments: true);
        }

        public async Task<ReviewerAssignmentResponse> AcceptAssignmentAsync(int assignmentId, int reviewerId)
        {
            var request = await _context.ReviewRequests
                .Include(r => r.Paper)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.ReviewRequestId == assignmentId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu phân công phản biện này.");

            if (request.ReviewerId != reviewerId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên bài phản biện này.");

            if (request.Status == "COMPLETED")
                throw new InvalidOperationException("Yêu cầu phản biện này đã hoàn thành trước đó.");

            if (request.Status == "DECLINED" || request.ConflictOfInterestDeclared == true)
                throw new InvalidOperationException("Yêu cầu phản biện này đã bị từ chối hoặc khai báo xung đột lợi ích.");

            request.Status = "UNDER_REVIEW";
            request.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify Admin
            if (request.AssignedByAdminId.HasValue)
            {
                await CreateNotificationAsync(
                    request.AssignedByAdminId.Value,
                    $"Phản biện viên {request.Reviewer?.FullName ?? "Reviewer"} đã chấp nhận lời mời phản biện bài báo \"{request.Paper?.Title}\"."
                );
            }

            return MapToAssignmentResponse(request, includePrivateAdminComments: true);
        }

        public async Task<ReviewerAssignmentResponse> DeclineAssignmentAsync(int assignmentId, int reviewerId, ReviewerDeclineRequest declineRequest)
        {
            var request = await _context.ReviewRequests
                .Include(r => r.Paper)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.ReviewRequestId == assignmentId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu phân công phản biện này.");

            if (request.ReviewerId != reviewerId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên bài phản biện này.");

            if (request.Status == "COMPLETED")
                throw new InvalidOperationException("Không thể từ chối vì bạn đã nộp bài đánh giá hoàn tất.");

            request.Status = "DECLINED";
            request.DeclinedAt = DateTime.UtcNow;
            request.ConflictOfInterestReason = declineRequest.Reason;

            await _context.SaveChangesAsync();

            // Notify Admin
            if (request.AssignedByAdminId.HasValue)
            {
                await CreateNotificationAsync(
                    request.AssignedByAdminId.Value,
                    $"Phản biện viên {request.Reviewer?.FullName ?? "Reviewer"} đã từ chối phản biện bài báo \"{request.Paper?.Title}\". Lý do: {declineRequest.Reason}"
                );
            }

            return MapToAssignmentResponse(request, includePrivateAdminComments: true);
        }

        public async Task<ReviewerAssignmentResponse> DeclareConflictOfInterestAsync(int assignmentId, int reviewerId, ReviewerConflictOfInterestRequest coiRequest)
        {
            var request = await _context.ReviewRequests
                .Include(r => r.Paper)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.ReviewRequestId == assignmentId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu phân công phản biện này.");

            if (request.ReviewerId != reviewerId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên bài phản biện này.");

            if (request.Status == "COMPLETED")
                throw new InvalidOperationException("Không thể khai báo xung đột lợi ích sau khi đã hoàn thành đánh giá.");

            request.Status = "DECLINED";
            request.DeclinedAt = DateTime.UtcNow;
            request.ConflictOfInterestDeclared = true;
            request.ConflictOfInterestReason = coiRequest.Reason;

            await _context.SaveChangesAsync();

            // Notify Admin
            if (request.AssignedByAdminId.HasValue)
            {
                await CreateNotificationAsync(
                    request.AssignedByAdminId.Value,
                    $"[CẢNH BÁO COI] Phản biện viên {request.Reviewer?.FullName ?? "Reviewer"} đã khai báo xung đột lợi ích đối với bài báo \"{request.Paper?.Title}\". Chi tiết: {coiRequest.Reason}"
                );
            }

            return MapToAssignmentResponse(request, includePrivateAdminComments: true);
        }

        public async Task<PaperReviewResponse> SubmitReviewAsync(int assignmentId, int reviewerId, PaperReviewSubmitRequest request)
        {
            var assignment = await _context.ReviewRequests
                .Include(r => r.Paper)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.ReviewRequestId == assignmentId);

            if (assignment == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu phân công phản biện này.");

            if (assignment.ReviewerId != reviewerId)
                throw new UnauthorizedAccessException("Bạn không có quyền đánh giá bài báo này.");

            if (assignment.Status == "DECLINED" || assignment.ConflictOfInterestDeclared == true)
                throw new InvalidOperationException("Bạn đã từ chối hoặc khai báo xung đột lợi ích với bài báo này.");

            // Chuẩn hóa recommendation
            var rec = (request.Recommendation ?? string.Empty).Trim().ToUpper();
            if (rec != "ACCEPT" && rec != "REVISION_REQUIRED" && rec != "REJECT")
            {
                throw new ArgumentException("Khuyến nghị kết quả phải là một trong: ACCEPT, REVISION_REQUIRED, hoặc REJECT.");
            }

            // Kiểm tra các trường bắt buộc theo rule học thuật
            if (rec == "REVISION_REQUIRED" && string.IsNullOrWhiteSpace(request.RequiredImprovements))
            {
                throw new ArgumentException("Khi khuyến nghị 'REVISION_REQUIRED', bạn bắt buộc phải chỉ rõ các yêu cầu cải thiện (RequiredImprovements).");
            }

            if (rec == "REJECT" && string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                throw new ArgumentException("Khi khuyến nghị 'REJECT', bạn bắt buộc phải giải thích lý do từ chối bài báo (RejectionReason).");
            }

            // Chuẩn bị danh sách 11 tiêu chí đánh giá
            var criteriaList = PrepareCriteria(request.Criteria);
            var criteriaJson = JsonSerializer.Serialize(criteriaList);

            // Tìm hoặc tạo DetailedEvaluation
            var evaluation = await _context.DetailedEvaluations
                .FirstOrDefaultAsync(e => e.ReviewRequestId == assignmentId);

            if (evaluation == null)
            {
                evaluation = new DetailedEvaluation
                {
                    ReviewRequestId = assignmentId,
                    ReviewerId = reviewerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.DetailedEvaluations.AddAsync(evaluation);
            }

            evaluation.Recommendation = rec;
            evaluation.FinalDecision = rec;
            evaluation.OverallSummary = request.OverallSummary;
            evaluation.Strengths = request.Strengths;
            evaluation.RequiredImprovements = request.RequiredImprovements;
            evaluation.RejectionReason = request.RejectionReason;
            evaluation.CommentsForResearcher = request.CommentsForResearcher;
            evaluation.GeneralComments = request.CommentsForResearcher;
            evaluation.PrivateCommentsForAdmin = request.PrivateCommentsForAdmin;
            evaluation.EthicsOrCopyrightConcern = request.EthicsOrCopyrightConcern;
            evaluation.ReviewedPaperVersion = request.ReviewedPaperVersion ?? "1.0";
            evaluation.SpecializedEvaluation = criteriaJson;

            // Map một số điểm số tiêu biểu sang các cột truyền thống để tương thích ngược
            var originalityItem = criteriaList.FirstOrDefault(c => c.CriterionCode == "ORIGINALITY");
            if (originalityItem != null && originalityItem.Rating > 0) evaluation.ScoreOriginality = originalityItem.Rating * 20;

            var methodologyItem = criteriaList.FirstOrDefault(c => c.CriterionCode == "METHODOLOGY");
            if (methodologyItem != null && methodologyItem.Rating > 0) evaluation.ScoreMethodology = methodologyItem.Rating * 20;

            var resultsItem = criteriaList.FirstOrDefault(c => c.CriterionCode == "EVIDENCE_RESULTS");
            if (resultsItem != null && resultsItem.Rating > 0) evaluation.ScoreResults = resultsItem.Rating * 20;

            // Cập nhật trạng thái của ReviewRequest thành COMPLETED
            assignment.Status = "COMPLETED";
            assignment.CompletedAt = DateTime.UtcNow;

            // Cập nhật bài báo sang UNDER_REVIEW nếu còn PENDING
            if (assignment.Paper != null && (assignment.Paper.Status == "PENDING" || string.IsNullOrWhiteSpace(assignment.Paper.Status)))
            {
                assignment.Paper.Status = "UNDER_REVIEW";
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo đến Admin
            if (assignment.AssignedByAdminId.HasValue)
            {
                await CreateNotificationAsync(
                    assignment.AssignedByAdminId.Value,
                    $"Phản biện viên {assignment.Reviewer?.FullName ?? "Reviewer"} đã hoàn thành đánh giá bài báo \"{assignment.Paper?.Title}\" với đề xuất: [{rec}]."
                );
            }

            return MapToReviewResponse(evaluation, assignment, includePrivateAdminComments: true);
        }

        // =========================================================================
        // 2. DÀNH CHO ADMIN (PHÊ DUYỆT & XUẤT BẢN)
        // =========================================================================

        public async Task<AdminPaperReviewsSummaryResponse> GetPaperReviewsSummaryForAdminAsync(int paperId)
        {
            var paper = await _context.Papers
                .Include(p => p.Creator)
                .Include(p => p.SubField)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            if (paper == null)
                throw new KeyNotFoundException($"Không tìm thấy bài báo ID {paperId}.");

            var assignments = await _context.ReviewRequests
                .Include(r => r.Reviewer)
                .Include(r => r.DetailedEvaluation)
                .Where(r => r.PaperId == paperId)
                .ToListAsync();

            var reviews = assignments
                .Where(a => a.DetailedEvaluation != null)
                .Select(a => MapToReviewResponse(a.DetailedEvaluation!, a, includePrivateAdminComments: true))
                .ToList();

            var summary = new AdminPaperReviewsSummaryResponse
            {
                PaperId = paper.PaperId,
                PaperTitle = paper.Title,
                PaperStatus = paper.Status ?? "PENDING",
                CreatorId = paper.CreatorId,
                AuthorName = paper.Creator?.FullName,
                SubFieldName = paper.SubField?.Name,
                TotalAssignments = assignments.Count,
                CompletedReviews = reviews.Count,
                AcceptCount = reviews.Count(r => r.Recommendation == "ACCEPT"),
                RevisionRequiredCount = reviews.Count(r => r.Recommendation == "REVISION_REQUIRED"),
                RejectCount = reviews.Count(r => r.Recommendation == "REJECT"),
                Reviews = reviews,
                Assignments = assignments.Select(a => MapToAssignmentResponse(a, includePrivateAdminComments: true)).ToList()
            };

            return summary;
        }

        public async Task<AdminPaperReviewsSummaryResponse> AdminPublishPaperAsync(int paperId, int adminId, AdminPublishPaperRequest request)
        {
            var paper = await _context.Papers
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            if (paper == null)
                throw new KeyNotFoundException($"Không tìm thấy bài báo ID {paperId}.");

            paper.Status = "PUBLISHED";
            paper.UpdatedAt = DateTime.UtcNow;

            // Cập nhật quyết định của Admin vào các bản đánh giá
            var evaluations = await _context.DetailedEvaluations
                .Include(e => e.ReviewRequest)
                .Where(e => e.ReviewRequest != null && e.ReviewRequest.PaperId == paperId)
                .ToListAsync();

            foreach (var eval in evaluations)
            {
                eval.AdminDecision = "PUBLISHED";
                eval.AdminDecisionNotes = request.Notes;
                eval.AdminDecisionAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Thông báo đến tác giả
            if (paper.CreatorId.HasValue)
            {
                await CreateNotificationAsync(
                    paper.CreatorId.Value,
                    $"Chúc mừng! Bài báo \"{paper.Title}\" của bạn đã được Ban biên tập ARS chính thức phê duyệt XUẤT BẢN (Published)."
                );
            }

            return await GetPaperReviewsSummaryForAdminAsync(paperId);
        }

        public async Task<AdminPaperReviewsSummaryResponse> AdminRequestRevisionAsync(int paperId, int adminId, AdminRequestRevisionRequest request)
        {
            var paper = await _context.Papers
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            if (paper == null)
                throw new KeyNotFoundException($"Không tìm thấy bài báo ID {paperId}.");

            paper.Status = "REVISION_REQUIRED";
            paper.UpdatedAt = DateTime.UtcNow;

            var evaluations = await _context.DetailedEvaluations
                .Include(e => e.ReviewRequest)
                .Where(e => e.ReviewRequest != null && e.ReviewRequest.PaperId == paperId)
                .ToListAsync();

            foreach (var eval in evaluations)
            {
                eval.AdminDecision = "REVISION_REQUIRED";
                eval.AdminDecisionNotes = request.AdminNotes;
                eval.AdminDecisionAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Thông báo đến tác giả
            if (paper.CreatorId.HasValue)
            {
                await CreateNotificationAsync(
                    paper.CreatorId.Value,
                    $"Bài báo \"{paper.Title}\" của bạn cần được CHỈNH SỬA & BỔ SUNG (Revision Required) theo yêu cầu của Ban biên tập: {request.AdminNotes}"
                );
            }

            return await GetPaperReviewsSummaryForAdminAsync(paperId);
        }

        public async Task<AdminPaperReviewsSummaryResponse> AdminRejectPaperAsync(int paperId, int adminId, AdminRejectPaperRequest request)
        {
            var paper = await _context.Papers
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            if (paper == null)
                throw new KeyNotFoundException($"Không tìm thấy bài báo ID {paperId}.");

            paper.Status = "REJECTED";
            paper.UpdatedAt = DateTime.UtcNow;

            var evaluations = await _context.DetailedEvaluations
                .Include(e => e.ReviewRequest)
                .Where(e => e.ReviewRequest != null && e.ReviewRequest.PaperId == paperId)
                .ToListAsync();

            foreach (var eval in evaluations)
            {
                eval.AdminDecision = "REJECTED";
                eval.AdminDecisionNotes = request.RejectionReason;
                eval.AdminDecisionAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Thông báo đến tác giả
            if (paper.CreatorId.HasValue)
            {
                await CreateNotificationAsync(
                    paper.CreatorId.Value,
                    $"Bài báo \"{paper.Title}\" của bạn đã bị TỪ CHỐI xuất bản trên ARS. Lý do: {request.RejectionReason}"
                );
            }

            return await GetPaperReviewsSummaryForAdminAsync(paperId);
        }

        // =========================================================================
        // 3. DÀNH CHO RESEARCHER (TÁC GIẢ BÀI BÁO)
        // =========================================================================

        public async Task<AuthorPaperReviewFeedbackResponse> GetAuthorPaperReviewsAsync(int paperId, int researcherId)
        {
            var paper = await _context.Papers
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            if (paper == null)
                throw new KeyNotFoundException($"Không tìm thấy bài báo ID {paperId}.");

            if (paper.CreatorId != researcherId)
                throw new UnauthorizedAccessException("Bạn chỉ có thể xem phản hồi đánh giá bài báo của chính bạn.");

            // Chỉ cho phép xem nếu Admin đã đưa ra quyết định
            var isReleased = paper.Status == "PUBLISHED" || paper.Status == "REVISION_REQUIRED" || paper.Status == "REJECTED";

            var result = new AuthorPaperReviewFeedbackResponse
            {
                PaperId = paper.PaperId,
                PaperTitle = paper.Title,
                PaperStatus = paper.Status ?? "PENDING",
                IsFeedbackReleased = isReleased
            };

            if (!isReleased)
            {
                result.AdminDecisionNotes = "Bài báo đang trong quá trình phản biện và xét duyệt bởi Ban biên tập.";
                return result;
            }

            var assignments = await _context.ReviewRequests
                .Include(r => r.Reviewer)
                .Include(r => r.DetailedEvaluation)
                .Where(r => r.PaperId == paperId && r.DetailedEvaluation != null)
                .ToListAsync();

            // TUYỆT ĐỐI BẢO MẬT: Không để lộ PrivateCommentsForAdmin cho tác giả!
            result.Reviews = assignments
                .Select(a => MapToReviewResponse(a.DetailedEvaluation!, a, includePrivateAdminComments: false))
                .ToList();

            var latestEval = assignments.Select(a => a.DetailedEvaluation).FirstOrDefault(e => !string.IsNullOrEmpty(e?.AdminDecisionNotes));
            result.AdminDecisionNotes = latestEval?.AdminDecisionNotes;
            result.DecisionDate = latestEval?.AdminDecisionAt ?? paper.UpdatedAt;

            return result;
        }

        // =========================================================================
        // HELPER METHODS
        // =========================================================================

        private List<ReviewCriterionItemResponse> PrepareCriteria(List<ReviewCriterionItemRequest>? requestCriteria)
        {
            var result = new List<ReviewCriterionItemResponse>();
            var lookup = (requestCriteria ?? new List<ReviewCriterionItemRequest>())
                .ToDictionary(c => c.CriterionCode.Trim().ToUpper(), c => c);

            foreach (var def in DefaultCriteria)
            {
                if (lookup.TryGetValue(def.Code, out var reqItem))
                {
                    result.Add(new ReviewCriterionItemResponse
                    {
                        CriterionCode = def.Code,
                        CriterionName = def.Name,
                        Rating = reqItem.Rating,
                        RatingText = reqItem.Rating == 0 ? "NOT_APPLICABLE" : reqItem.Rating.ToString(),
                        Rationale = reqItem.Rationale
                    });
                }
                else
                {
                    result.Add(new ReviewCriterionItemResponse
                    {
                        CriterionCode = def.Code,
                        CriterionName = def.Name,
                        Rating = 3,
                        RatingText = "3",
                        Rationale = null
                    });
                }
            }

            return result;
        }

        private ReviewerAssignmentResponse MapToAssignmentResponse(ReviewRequest r, bool includePrivateAdminComments)
        {
            return new ReviewerAssignmentResponse
            {
                ReviewRequestId = r.ReviewRequestId,
                PaperId = r.PaperId ?? 0,
                PaperTitle = r.Paper?.Title,
                PaperAbstract = r.Paper?.Abstract,
                PaperFileUrl = r.Paper?.FileUrl,
                SubFieldId = r.Paper?.SubFieldId,
                SubFieldName = r.Paper?.SubField?.Name,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer?.FullName,
                Status = r.Status,
                Deadline = r.Deadline,
                CreatedAt = r.CreatedAt,
                AcceptedAt = r.AcceptedAt,
                DeclinedAt = r.DeclinedAt,
                CompletedAt = r.CompletedAt,
                ConflictOfInterestDeclared = r.ConflictOfInterestDeclared,
                ConflictOfInterestReason = r.ConflictOfInterestReason,
                Review = r.DetailedEvaluation == null ? null : MapToReviewResponse(r.DetailedEvaluation, r, includePrivateAdminComments)
            };
        }

        private PaperReviewResponse MapToReviewResponse(DetailedEvaluation eval, ReviewRequest assignment, bool includePrivateAdminComments)
        {
            List<ReviewCriterionItemResponse> criteria = new();
            if (!string.IsNullOrWhiteSpace(eval.SpecializedEvaluation) && eval.SpecializedEvaluation != "[]")
            {
                try
                {
                    criteria = JsonSerializer.Deserialize<List<ReviewCriterionItemResponse>>(eval.SpecializedEvaluation) ?? new();
                }
                catch
                {
                    criteria = new();
                }
            }

            return new PaperReviewResponse
            {
                DetailedEvaluationId = eval.DetailedEvaluationId,
                ReviewRequestId = eval.ReviewRequestId ?? assignment.ReviewRequestId,
                PaperId = assignment.PaperId ?? 0,
                PaperTitle = assignment.Paper?.Title,
                ReviewerId = eval.ReviewerId ?? assignment.ReviewerId,
                ReviewerName = assignment.Reviewer?.FullName,
                Recommendation = eval.Recommendation ?? eval.FinalDecision,
                OverallSummary = eval.OverallSummary,
                Strengths = eval.Strengths,
                RequiredImprovements = eval.RequiredImprovements,
                RejectionReason = eval.RejectionReason,
                CommentsForResearcher = eval.CommentsForResearcher ?? eval.GeneralComments,
                // BẢO MẬT: Chỉ đưa privateCommentsForAdmin khi authorized (Admin/Reviewer)
                PrivateCommentsForAdmin = includePrivateAdminComments ? eval.PrivateCommentsForAdmin : null,
                EthicsOrCopyrightConcern = eval.EthicsOrCopyrightConcern,
                ReviewedPaperVersion = eval.ReviewedPaperVersion ?? "1.0",
                SubmittedAt = assignment.CompletedAt ?? eval.CreatedAt,
                AdminDecision = eval.AdminDecision,
                AdminDecisionNotes = eval.AdminDecisionNotes,
                AdminDecisionAt = eval.AdminDecisionAt,
                Criteria = criteria
            };
        }

        private async Task CreateNotificationAsync(int userId, string message)
        {
            try
            {
                var noti = new Notification
                {
                    UserId = userId,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(noti);
                await _notificationRepository.SaveChangesAsync();
            }
            catch
            {
                // Bỏ qua lỗi notification nếu có để không cản trở flow chính
            }
        }
    }
}
