using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ForumPostCreateRequest
    {
        public string? Title { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? Abstract { get; set; }

        public string? Category { get; set; }

        public List<string> Tags { get; set; }
            = new List<string>();

        public string? AttachedPdfUrl { get; set; }

        public string? AttachedImageUrl { get; set; }
    }
}