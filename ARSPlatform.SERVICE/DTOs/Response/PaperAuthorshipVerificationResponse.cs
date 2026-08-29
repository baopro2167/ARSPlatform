using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PaperAuthorshipVerificationResponse
    {
        public int PaperId { get; set; }

        public string? PaperStatus { get; set; }

        public string? OpenAlexWorkId { get; set; }

        public string AuthorshipVerificationStatus { get; set; } = string.Empty;

        public DateTime? AuthorshipVerifiedAt { get; set; }

        public string? AuthorshipVerificationReason { get; set; }

        public string? VerifiedOrcidId { get; set; }

        public string? OrcidDisplayName { get; set; }

        public bool IsOrcidMatch { get; set; }

        public bool? IsNameMatch { get; set; }

        public string? MatchSource { get; set; }

        public string? MatchedAuthorName { get; set; }

        public string OpenAlexLookupStatus { get; set; } = string.Empty;

        public string? OpenAlexMessage { get; set; }

        public OpenAlexWorkResponse? Work { get; set; }

        public List<OpenAlexWorkAuthorshipResponse> Authorships { get; set; } = new();
    }
}