using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ManualAssignReviewersRequest
    {
        [Required(ErrorMessage = "PaperId là bắt buộc.")]
        public int PaperId { get; set; }

        /// <summary>
        /// Danh sách ID của các Reviewer cần gán (Ví dụ: [147, 148, 149])
        /// </summary>
        public List<int>? ReviewerIds { get; set; } = new List<int>();

        /// <summary>
        /// Hỗ trợ gán Reviewer thứ 1 riêng lẻ nếu FE gửi dạng từng trường
        /// </summary>
        public int? ReviewerId1 { get; set; }

        /// <summary>
        /// Hỗ trợ gán Reviewer thứ 2 riêng lẻ nếu FE gửi dạng từng trường
        /// </summary>
        public int? ReviewerId2 { get; set; }

        /// <summary>
        /// Hỗ trợ gán Reviewer thứ 3 riêng lẻ nếu FE gửi dạng từng trường
        /// </summary>
        public int? ReviewerId3 { get; set; }

        /// <summary>
        /// Hạn chót phản biện (tuỳ chọn, mặc định 14 ngày kể từ ngày gán)
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Thù lao phản biện (tuỳ chọn, nếu để trống sẽ tự lấy theo hồ sơ ReviewFee của Reviewer)
        /// </summary>
        public decimal? Fee { get; set; }

        /// <summary>
        /// Ghi chú / lời nhắn kèm theo cho Reviewer (tuỳ chọn)
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Lấy danh sách ID Reviewer không trùng lặp và hợp lệ (> 0)
        /// </summary>
        public List<int> GetDistinctReviewerIds()
        {
            var ids = new HashSet<int>();
            if (ReviewerIds != null)
            {
                foreach (var id in ReviewerIds)
                {
                    if (id > 0) ids.Add(id);
                }
            }
            if (ReviewerId1.HasValue && ReviewerId1.Value > 0) ids.Add(ReviewerId1.Value);
            if (ReviewerId2.HasValue && ReviewerId2.Value > 0) ids.Add(ReviewerId2.Value);
            if (ReviewerId3.HasValue && ReviewerId3.Value > 0) ids.Add(ReviewerId3.Value);
            return ids.ToList();
        }
    }
}
