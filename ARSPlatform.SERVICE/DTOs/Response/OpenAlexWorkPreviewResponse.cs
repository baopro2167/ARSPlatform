using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OpenAlexWorkPreviewResponse
    {
        public string OpenAlexWorkId { get; set; }
            = string.Empty;

        public string LookupStatus { get; set; }
            = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public string? Title { get; set; }

        public string? Abstract { get; set; }

        public int? PublicationYear { get; set; }

        public DateTime? PublicationDate { get; set; }

        public string? Doi { get; set; }

        public string? Type { get; set; }

        public int CitedByCount { get; set; }

        public bool IsRetracted { get; set; }

        public bool? IsOpenAccess { get; set; }

        public string? OpenAccessStatus { get; set; }

        public List<OpenAlexWorkPreviewAuthorResponse> Authors { get; set; }
            = new List<OpenAlexWorkPreviewAuthorResponse>();

        public List<OpenAlexWorkPreviewTopicResponse> Topics { get; set; }
            = new List<OpenAlexWorkPreviewTopicResponse>();

        public List<OpenAlexWorkPreviewConceptResponse> Concepts { get; set; }
            = new List<OpenAlexWorkPreviewConceptResponse>();

        public OpenAlexWorkPreviewSourceResponse? Source { get; set; }

        public string? ExternalUrl { get; set; }

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    public class OpenAlexWorkPreviewAuthorResponse
    {
        public string? RawAuthorName { get; set; }

        public string? RawOrcid { get; set; }

        public string? AuthorOpenAlexId { get; set; }

        public string? AuthorDisplayName { get; set; }

        public string? AuthorOrcid { get; set; }

        public bool? IsCorresponding { get; set; }

        public List<OpenAlexWorkPreviewInstitutionResponse> Institutions { get; set; }
            = new List<OpenAlexWorkPreviewInstitutionResponse>();
    }

    public class OpenAlexWorkPreviewInstitutionResponse
    {
        public string? OpenAlexId { get; set; }

        public string? DisplayName { get; set; }

        public string? Ror { get; set; }

        public string? CountryCode { get; set; }

        public string? Type { get; set; }
    }

    public class OpenAlexWorkPreviewTopicResponse
    {
        public string? TopicId { get; set; }

        public string? TopicName { get; set; }

        public double? Score { get; set; }

        public string? SubFieldId { get; set; }

        public string? SubFieldName { get; set; }

        public string? FieldId { get; set; }

        public string? FieldName { get; set; }

        public string? DomainId { get; set; }

        public string? DomainName { get; set; }
    }

    public class OpenAlexWorkPreviewConceptResponse
    {
        public string? ConceptId { get; set; }

        public string? ConceptName { get; set; }

        public double? Score { get; set; }

        public int? Level { get; set; }
    }

    public class OpenAlexWorkPreviewSourceResponse
    {
        public string? OpenAlexSourceId { get; set; }

        public string? DisplayName { get; set; }

        public string? IssnL { get; set; }

        public List<string> Issns { get; set; }
            = new List<string>();

        public string? Type { get; set; }

        public string? HostOrganizationOpenAlexId { get; set; }
    }
}