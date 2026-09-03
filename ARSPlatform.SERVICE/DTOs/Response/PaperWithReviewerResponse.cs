using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    /// <summary>
    /// Thông tin paper kèm reviewer được phân công.
    /// Dùng cho API <c>GET /api/Paper/by-reviewer/{reviewerId}</c>.
    /// </summary>
    public class PaperWithReviewerResponse
    {
        public int PaperId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Abstract { get; set; }

        public string? FileUrl { get; set; }

        public string? Status { get; set; }

        /// <summary>
        /// Loại bài báo: <c>Journal</c> hoặc <c>Conference</c>.
        /// </summary>
        public string PaperType { get; set; } = "Journal";

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PublicationDate { get; set; }

        public string? Quartile { get; set; }

        public string? SourceName { get; set; }

        public string? Doi { get; set; }

        public int? SubFieldId { get; set; }

        public int? AuthorId { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// ID phản biện viên được phân công cho paper (ReviewRequest.ReviewerId).
        /// </summary>
        public int ReviewerId { get; set; }

        /// <summary>
        /// Tên đầy đủ của phản biện viên (User.FullName).
        /// </summary>
        public string ReviewerName { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái yêu cầu review (ReviewRequest.Status).
        /// </summary>
        public string? ReviewRequestStatus { get; set; }

        public int ReviewRequestId { get; set; }
    }
}
