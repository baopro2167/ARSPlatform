using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    /// <summary>
    /// Trạng thái lookup trả về cho FE để hiển thị thông báo phù hợp.
    /// </summary>
    public static class ScholarLookupStatus
    {
        public const string Found = "Found";
        public const string NotFound = "NotFound";
        public const string InvalidAuthorId = "InvalidAuthorId";
        public const string InvalidQuery = "InvalidQuery";
        public const string RateLimited = "RateLimited";
        public const string ProviderUnavailable = "ProviderUnavailable";
        public const string ProviderTimeout = "ProviderTimeout";
        public const string ProviderError = "ProviderError";
    }

    /// <summary>
    /// Profile + chỉ số trích dẫn của một tác giả từ Semantic Scholar.
    /// </summary>
    public class ScholarAuthorProfileResponse
    {
        public string AuthorId { get; set; } = string.Empty;

        public string LookupStatus { get; set; } = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public string? Name { get; set; }

        public List<string> Affiliations { get; set; } = new();

        public string? Homepage { get; set; }

        public int PaperCount { get; set; }

        public int CitationCount { get; set; }

        public int? HIndex { get; set; }

        public string? ExternalUrl { get; set; }

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    /// <summary>
    /// Kết quả search tác giả theo tên.
    /// </summary>
    public class ScholarSearchResponse
    {
        public string Query { get; set; } = string.Empty;

        public string LookupStatus { get; set; } = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public int Total { get; set; }

        public int Offset { get; set; }

        public int? Next { get; set; }

        public List<ScholarAuthorSummaryResponse> Data { get; set; } = new();

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    /// <summary>
    /// Tóm tắt 1 tác giả trong kết quả search (ít field hơn profile).
    /// </summary>
    public class ScholarAuthorSummaryResponse
    {
        public string AuthorId { get; set; } = string.Empty;

        public string? Name { get; set; }

        public List<string> Affiliations { get; set; } = new();

        public int? PaperCount { get; set; }

        public int? CitationCount { get; set; }

        public int? HIndex { get; set; }
    }

    /// <summary>
    /// Danh sách bài báo của một tác giả.
    /// </summary>
    public class ScholarAuthorPapersResponse
    {
        public string AuthorId { get; set; } = string.Empty;

        public string LookupStatus { get; set; } = string.Empty;

        public DateTime SourceFetchedAt { get; set; }

        public int Offset { get; set; }

        public int? Next { get; set; }

        public List<ScholarPaperResponse> Data { get; set; } = new();

        public string? Message { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    /// <summary>
    /// Thông tin một bài báo từ Semantic Scholar.
    /// </summary>
    public class ScholarPaperResponse
    {
        public string? PaperId { get; set; }

        public string? Title { get; set; }

        public string? Abstract { get; set; }

        public int? Year { get; set; }

        public string? Venue { get; set; }

        public int? CitationCount { get; set; }

        public int? ReferenceCount { get; set; }

        public int? InfluentialCitationCount { get; set; }

        public string? Doi { get; set; }

        public string? ArxivId { get; set; }

        public string? ExternalIds { get; set; }

        public string? ExternalUrl { get; set; }

        public List<ScholarPaperAuthorRef> Authors { get; set; } = new();
    }

    /// <summary>
    /// Tham chiếu tác giả trong một bài báo.
    /// </summary>
    public class ScholarPaperAuthorRef
    {
        public string? AuthorId { get; set; }

        public string? Name { get; set; }
    }
}
