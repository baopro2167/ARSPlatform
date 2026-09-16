using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request body cho endpoint admin bật / tắt 1 bản ghi UserMedal (cấp huy hiệu cho user).
    /// Body JSON ví dụ: { "status": "Active" } hoặc { "status": "Inactive" }.
    /// </summary>
    public class MedalUserMedalStatusUpdateRequest
    {
        /// <summary>
        /// Trạng thái mới: "Active" hoặc "Inactive" (không phân biệt hoa thường).
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
