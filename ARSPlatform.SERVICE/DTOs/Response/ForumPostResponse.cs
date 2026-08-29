using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ForumPostResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string? AuthorAvatar { get; set; }

        public DateTime Timestamp { get; set; }

        public string? Abstract { get; set; }

        public List<string> Tags { get; set; }
            = new List<string>();

        public int Likes { get; set; }

        public int Comments { get; set; }

        public int Views { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? AttachedPdfUrl { get; set; }

        public string? AttachedImageUrl { get; set; }

        public int AuthorId { get; set; }

        public bool IsLiked { get; set; } = false;
    }
}