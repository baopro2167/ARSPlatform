using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperUpdateRequest
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Abstract { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(30)]
        public string? Status { get; set; }

        public bool? Issn { get; set; }

        public bool? IsOpenAccess { get; set; }

        [MaxLength(50)]
        public string? Quartile { get; set; }

        public int? SubFieldId { get; set; }

        /// <summary>
        /// Loại bài báo. Bắt buộc, chỉ chấp nhận 1 trong 2 giá trị:
        /// <list type="bullet">
        /// <item><description><c>Journal</c> - Bài báo tạp chí khoa học.</description></item>
        /// <item><description><c>Conference</c> - Bài báo hội nghị khoa học.</description></item>
        /// </list>
        /// </summary>
        [Required(ErrorMessage = "PaperType là bắt buộc. Chỉ chấp nhận 'Journal' hoặc 'Conference'.")]
        [MaxLength(20)]
        [RegularExpression("^(Journal|Conference)$",
            ErrorMessage = "PaperType chỉ chấp nhận 'Journal' hoặc 'Conference'.")]
        public string PaperType { get; set; } = "Journal";

        [MaxLength(50)]
        [RegularExpression(@"^W[0-9]+$", ErrorMessage = "OpenAlexWorkId must be a canonical W-prefixed OpenAlex Work ID.")]
        public string? OpenAlexWorkId { get; set; }

        [MaxLength(255)]
        public string? Doi { get; set; }

        public DateTime? PublicationDate { get; set; }

        [MaxLength(255)]
        public string? SourceName { get; set; }

        [MaxLength(50)]
        public string? IssnValue { get; set; }

        public List<PaperAuthorRequest>? Authors { get; set; }
    }
}
