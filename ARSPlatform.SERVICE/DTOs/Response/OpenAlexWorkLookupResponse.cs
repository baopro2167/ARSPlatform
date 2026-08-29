using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OpenAlexWorkLookupResponse
    {
        public string OpenAlexWorkId { get; set; }
            = string.Empty;

        public string LookupStatus { get; set; }
            = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public OpenAlexWorkResponse? Work { get; set; }

        public List<OpenAlexWorkAuthorshipResponse> Authorships { get; set; }
            = new List<OpenAlexWorkAuthorshipResponse>();

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    public class OpenAlexWorkAuthorshipResponse
    {
        public string? RawAuthorName { get; set; }

        public string? RawOrcid { get; set; }

        public string? AuthorOpenAlexId { get; set; }

        public string? AuthorDisplayName { get; set; }

        public string? AuthorOrcid { get; set; }

        public bool? IsCorresponding { get; set; }
    }
}