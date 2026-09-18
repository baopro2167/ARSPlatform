using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request body dành riêng để Admin cập nhật GradingRubric của một SubField.
    /// Không thay đổi Name, MajorFieldId, Description hay bất kỳ trường nào khác.
    /// </summary>
    public class SubFieldRubricUpdateRequest
    {
        /// <summary>
        /// Danh sách tiêu chí chấm điểm. Hệ thống chấp nhận 2 nguồn:
        ///   1. Hệ thống cung cấp sẵn (Admin chọn template mặc định do hệ thống định nghĩa).
        ///   2. Admin tự soạn thảo theo nhu cầu cá nhân / hội đồng.
        /// Bắt buộc phải có ít nhất 1 tiêu chí; nếu gửi danh sách rỗng sẽ bị từ chối.
        /// </summary>
        [Required(ErrorMessage = "GradingRubric is required.")]
        [MinLength(1, ErrorMessage = "GradingRubric must have at least 1 criterion.")]
        public List<GradingRubricCriterionRequest> GradingRubric { get; set; }
            = new List<GradingRubricCriterionRequest>();
    }
}
