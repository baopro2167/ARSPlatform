using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[+\d\s\-()]{8,20}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        [Url]
        public string PdfUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? OrcidTicket { get; set; }

        /// <summary>
        /// Mã định danh OpenAlex (ví dụ: https://openalex.org/A5023888391 hoặc A5023888391).
        /// Áp dụng cho Giảng viên (Lecturer), Nhà nghiên cứu (Researcher), Phản biện (Reviewer).
        /// </summary>
        [MaxLength(255)]
        public string? OpenAlexId { get; set; }

        /// <summary>
        /// Mã định danh Semantic Scholar (ví dụ: 1741101 hoặc https://www.semanticscholar.org/author/1741101).
        /// Áp dụng cho Giảng viên (Lecturer), Nhà nghiên cứu (Researcher), Phản biện (Reviewer) - Bắt buộc nhập nếu không có OpenAlexId.
        /// </summary>
        [MaxLength(255)]
        public string? SemanticScholarId { get; set; }
    }
}