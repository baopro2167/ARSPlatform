using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperAuthorshipDecisionRequest
    {
        /// <summary>
        /// Quyết định xác minh: "VERIFIED" hoặc "REJECTED" (chấp nhận cả ALLOW, ALLOWED, APPROVED, REJECT, DENIED).
        /// </summary>
        [Required(ErrorMessage = "Decision is required ('VERIFIED' or 'REJECTED').")]
        public string Decision { get; set; } = string.Empty;

        /// <summary>
        /// Lý do xác minh hoặc từ chối. Tối đa 100 ký tự.
        /// </summary>
        [MaxLength(100)]
        public string? Reason { get; set; }
    }
}
