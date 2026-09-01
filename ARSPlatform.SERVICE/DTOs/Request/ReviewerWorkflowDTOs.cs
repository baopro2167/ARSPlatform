using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ReviewerDeclineRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối phản biện.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewerConflictOfInterestRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập chi tiết xung đột lợi ích.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewCriterionItemRequest
    {
        [Required]
        public string CriterionCode { get; set; } = string.Empty;

        public string? CriterionName { get; set; }

        /// <summary>
        /// Điểm đánh giá: 1..5 hoặc 0 nếu Không áp dụng (NOT_APPLICABLE)
        /// </summary>
        [Range(0, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 hoặc 0 (NOT_APPLICABLE).")]
        public int Rating { get; set; } = 3;

        public string? Rationale { get; set; }
    }

    public class PaperReviewSubmitRequest
    {
        /// <summary>
        /// Khuyến nghị kết quả: ACCEPT, REVISION_REQUIRED, REJECT
        /// </summary>
        [Required(ErrorMessage = "Vui lòng chọn khuyến nghị kết quả (ACCEPT, REVISION_REQUIRED hoặc REJECT).")]
        public string Recommendation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tóm tắt tổng quan bài báo.")]
        public string OverallSummary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nêu các điểm mạnh của bài báo.")]
        public string Strengths { get; set; } = string.Empty;

        /// <summary>
        /// Bắt buộc nhập nếu Recommendation là REVISION_REQUIRED
        /// </summary>
        public string? RequiredImprovements { get; set; }

        /// <summary>
        /// Bắt buộc nhập nếu Recommendation là REJECT
        /// </summary>
        public string? RejectionReason { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nhận xét gửi tác giả.")]
        public string CommentsForResearcher { get; set; } = string.Empty;

        /// <summary>
        /// Bình luận bảo mật riêng cho Admin / Ban biên tập
        /// </summary>
        public string? PrivateCommentsForAdmin { get; set; }

        public bool EthicsOrCopyrightConcern { get; set; } = false;

        public string? ReviewedPaperVersion { get; set; } = "1.0";

        public List<ReviewCriterionItemRequest> Criteria { get; set; } = new List<ReviewCriterionItemRequest>();
    }

    public class AdminPublishPaperRequest
    {
        public string? Notes { get; set; }
    }

    public class AdminRequestRevisionRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập yêu cầu chỉnh sửa gửi tác giả.")]
        public string AdminNotes { get; set; } = string.Empty;

        public bool ReleaseFeedbackToAuthor { get; set; } = true;
    }

    public class AdminRejectPaperRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối bài báo.")]
        public string RejectionReason { get; set; } = string.Empty;

        public bool ReleaseFeedbackToAuthor { get; set; } = true;
    }
}
