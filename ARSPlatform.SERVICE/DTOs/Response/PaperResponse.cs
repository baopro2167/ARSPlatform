using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PaperResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public string? FileUrl { get; set; }
        public bool? Issn { get; set; }
        public bool? IsOpenAccess { get; set; }
        public string? Quartile { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? SubFieldId { get; set; }
        public int? AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
