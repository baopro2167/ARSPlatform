using System;

namespace ARSPlatform.MODEL.Entities
{
    public class Paper
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string? Doi { get; set; }
        public string? FileUrl { get; set; }
        public string Status { get; set; } = "Submitted";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Key
        public Guid AuthorId { get; set; }
        public virtual User Author { get; set; } = null!;
    }
}
