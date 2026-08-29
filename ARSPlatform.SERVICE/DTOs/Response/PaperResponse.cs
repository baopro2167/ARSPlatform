using System;
using System.Collections.Generic;

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

        public string? AuthorOrcidId { get; set; }

        public string? AuthorOrcidDisplayName { get; set; }

        public bool AuthorIsOrcidVerified { get; set; }

        public string? OpenAlexWorkId { get; set; }

        public string? Doi { get; set; }

        public DateTime? PublicationDate { get; set; }

        public string? SourceName { get; set; }

        public string? IssnValue { get; set; }

        public string AuthorshipVerificationStatus { get; set; }
            = "NOT_CHECKED";

        public DateTime? AuthorshipVerifiedAt { get; set; }

        public string? AuthorshipVerificationReason { get; set; }

        public List<PaperAuthorResponse> Authors { get; set; } = new List<PaperAuthorResponse>();
    }
}