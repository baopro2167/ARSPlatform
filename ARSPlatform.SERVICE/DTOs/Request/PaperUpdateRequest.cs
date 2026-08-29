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