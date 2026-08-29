using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PaperAuthorResponse
    {
        public int PaperAuthorId { get; set; }

        public int AuthorOrder { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public string? RawAuthorName { get; set; }

        public string? OrcidId { get; set; }

        public string? OpenAlexAuthorId { get; set; }

        public bool? IsCorresponding { get; set; }

        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}