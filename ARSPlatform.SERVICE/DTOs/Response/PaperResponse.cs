using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PaperResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string? Doi { get; set; }
        public string? FileUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
