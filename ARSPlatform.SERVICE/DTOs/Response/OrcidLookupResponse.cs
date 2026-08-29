using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OrcidLookupResponse
    {
        public string OrcidId { get; set; } = string.Empty;

        public string LookupStatus { get; set; } = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public OpenAlexAuthorResponse? Author { get; set; }

        public OpenAlexMetricsResponse? Metrics { get; set; }

        public List<OpenAlexAffiliationResponse> Affiliations { get; set; }
            = new List<OpenAlexAffiliationResponse>();

        public List<OpenAlexInstitutionResponse> LastKnownInstitutions { get; set; }
            = new List<OpenAlexInstitutionResponse>();

        public List<OpenAlexTopicResponse> Topics { get; set; }
            = new List<OpenAlexTopicResponse>();

        public List<OpenAlexYearCountResponse> CountsByYear { get; set; }
            = new List<OpenAlexYearCountResponse>();

        public List<OpenAlexWorkResponse> Works { get; set; }
            = new List<OpenAlexWorkResponse>();

        public List<string> MissingSections { get; set; }
            = new List<string>();

        public List<string> ProviderWarnings { get; set; }
            = new List<string>();

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    public class OpenAlexAuthorResponse
    {
        public string? OpenAlexId { get; set; }

        public string? Orcid { get; set; }

        public string? DisplayName { get; set; }

        public string? FullName { get; set; }

        public List<string> AlternativeNames { get; set; }
            = new List<string>();

        public List<string> RawAuthorNames { get; set; }
            = new List<string>();

        public string? ExternalUrl { get; set; }
    }

    public class OpenAlexMetricsResponse
    {
        public int WorksCount { get; set; }

        public int CitedByCount { get; set; }

        public int? HIndex { get; set; }

        public int? I10Index { get; set; }

        public double? TwoYearMeanCitedness { get; set; }
    }

    public class OpenAlexAffiliationResponse
    {
        public string? InstitutionOpenAlexId { get; set; }

        public string? InstitutionName { get; set; }

        public string? Ror { get; set; }

        public string? CountryCode { get; set; }

        public string? Type { get; set; }

        public List<int> Years { get; set; }
            = new List<int>();
    }

    public class OpenAlexInstitutionResponse
    {
        public string? OpenAlexId { get; set; }

        public string? DisplayName { get; set; }

        public string? Ror { get; set; }

        public string? CountryCode { get; set; }

        public string? Type { get; set; }
    }

    public class OpenAlexTopicResponse
    {
        public string? TopicId { get; set; }

        public string? TopicName { get; set; }

        public int Count { get; set; }

        public string? SubFieldId { get; set; }

        public string? SubFieldName { get; set; }

        public string? FieldId { get; set; }

        public string? FieldName { get; set; }

        public string? DomainId { get; set; }

        public string? DomainName { get; set; }
    }

    public class OpenAlexYearCountResponse
    {
        public int Year { get; set; }

        public int WorksCount { get; set; }

        public int OaWorksCount { get; set; }

        public int CitedByCount { get; set; }
    }

    public class OpenAlexWorkResponse
    {
        public string? OpenAlexId { get; set; }

        public string? Title { get; set; }

        public string? Doi { get; set; }

        public int? PublicationYear { get; set; }

        public DateTime? PublicationDate { get; set; }

        public string? Type { get; set; }

        public int CitedByCount { get; set; }

        public string? SourceName { get; set; }

        public bool? IsOpenAccess { get; set; }

        public string? OpenAccessStatus { get; set; }

        public bool IsRetracted { get; set; }

        public string? ExternalUrl { get; set; }
    }
}