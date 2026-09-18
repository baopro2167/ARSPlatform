using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PaperAuthorshipDecisionResponse
    {
        public int Id { get; set; }

        public int PaperId => Id;

        public string Status { get; set; } = string.Empty;

        public string AuthorshipVerificationStatus { get; set; } = string.Empty;

        public DateTime? AuthorshipVerifiedAt { get; set; }

        public string? AuthorshipVerificationReason { get; set; }
    }
}
