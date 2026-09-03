using System;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Giảng viên (Lecture) gia hạn deadline để sinh viên nộp bài báo cáo tiến độ.
    /// API sẽ tự động set <c>Status = "Pending"</c> cho báo cáo.
    /// </summary>
    public class PhasedReportExtendDeadlineRequest
    {
        /// <summary>
        /// Hạn nộp mới (UTC). Bắt buộc, phải lớn hơn thời điểm hiện tại.
        /// </summary>
        [Required(ErrorMessage = "Deadline là bắt buộc.")]
        public DateTime DeadlineAt { get; set; }
    }
}
