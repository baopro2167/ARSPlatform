using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class SemanticScholarService : ISemanticScholarService
    {
        private const string Found = ScholarLookupStatus.Found;
        private const string NotFound = ScholarLookupStatus.NotFound;
        private const string InvalidAuthorId = ScholarLookupStatus.InvalidAuthorId;
        private const string InvalidQuery = ScholarLookupStatus.InvalidQuery;
        private const string RateLimited = ScholarLookupStatus.RateLimited;
        private const string ProviderUnavailable = ScholarLookupStatus.ProviderUnavailable;
        private const string ProviderTimeout = ScholarLookupStatus.ProviderTimeout;
        private const string ProviderError = ScholarLookupStatus.ProviderError;

        // Fields tối thiểu cho author profile
        private const string AuthorProfileFields =
            "authorId,name,affiliations,paperCount,citationCount,hIndex,homepage,externalIds";

        // Fields cho search author
        private const string AuthorSearchFields =
            "authorId,name,affiliations,paperCount,citationCount,hIndex";

        // Fields cho paper list
        private const string PaperFields =
            "paperId,title,abstract,year,venue,citationCount,referenceCount," +
            "influentialCitationCount,externalIds,authors";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly SemanticScholarSettings _settings;
        private readonly ILogger<SemanticScholarService> _logger;
        private readonly IMemoryCache _cache;

        public SemanticScholarService(
            HttpClient httpClient,
            IOptions<SemanticScholarSettings> settings,
            ILogger<SemanticScholarService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ScholarAuthorProfileResponse> GetAuthorAsync(
            string authorId,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidAuthorId(authorId, out var normalizedAuthorId))
            {
                return ProfileFailure(
                    authorId?.Trim() ?? string.Empty,
                    InvalidAuthorId,
                    "The supplied Semantic Scholar Author ID is invalid.");
            }

            var cacheKey = $"scholar:author:{normalizedAuthorId}";

            if (_cache.TryGetValue(
                    cacheKey,
                    out ScholarAuthorProfileResponse? cached) &&
                cached != null)
            {
                return cached;
            }

            try
            {
                var fields = Uri.EscapeDataString(AuthorProfileFields);
                var requestUri = $"author/{normalizedAuthorId}?fields={fields}";

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var fetchedAt = DateTime.UtcNow;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return ProfileFailure(
                        normalizedAuthorId,
                        NotFound,
                        "No Semantic Scholar author was found for this Author ID.",
                        fetchedAt);
                }

                if ((int)response.StatusCode == 429)
                {
                    return ProfileFailure(
                        normalizedAuthorId,
                        RateLimited,
                        "Semantic Scholar author lookup is temporarily rate limited.",
                        fetchedAt,
                        GetRetryAfterSeconds(response));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning(
                        "Semantic Scholar author lookup failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (response.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        return ProfileFailure(
                            normalizedAuthorId,
                            ProviderTimeout,
                            "Semantic Scholar author lookup timed out.",
                            fetchedAt);
                    }

                    if (statusCode >= 500)
                    {
                        return ProfileFailure(
                            normalizedAuthorId,
                            ProviderUnavailable,
                            "Semantic Scholar is temporarily unavailable.",
                            fetchedAt);
                    }

                    return ProfileFailure(
                        normalizedAuthorId,
                        ProviderError,
                        "Semantic Scholar returned an unexpected response.",
                        fetchedAt);
                }

                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                var author = await JsonSerializer
                    .DeserializeAsync<ScholarAuthorApiResponse>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                if (author == null ||
                    string.IsNullOrWhiteSpace(author.AuthorId))
                {
                    _logger.LogWarning(
                        "Semantic Scholar author lookup returned a successful response without an author ID.");

                    return ProfileFailure(
                        normalizedAuthorId,
                        ProviderError,
                        "Semantic Scholar returned incomplete author metadata.",
                        fetchedAt);
                }

                var result = MapAuthorProfile(author, fetchedAt);

                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(
                        Math.Clamp(_settings.AuthorCacheSeconds, 30, 86400)));

                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Semantic Scholar author lookup timed out.");

                return ProfileFailure(
                    normalizedAuthorId,
                    ProviderTimeout,
                    "Semantic Scholar author lookup timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar author lookup failed because of a network or transport error.");

                return ProfileFailure(
                    normalizedAuthorId,
                    ProviderUnavailable,
                    "Semantic Scholar is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar author lookup returned malformed JSON.");

                return ProfileFailure(
                    normalizedAuthorId,
                    ProviderError,
                    "Semantic Scholar returned an invalid response.");
            }
        }

        public async Task<ScholarSearchResponse> SearchAuthorsAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var normalizedQuery = query?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return new ScholarSearchResponse
                {
                    Query = normalizedQuery,
                    LookupStatus = InvalidQuery,
                    SourceFetchedAt = DateTime.UtcNow,
                    Message = "The search query must not be empty."
                };
            }

            var effectiveLimit = Math.Clamp(limit, 1, 1000);

            var cacheKey = $"scholar:author-search:{normalizedQuery}:{effectiveLimit}";

            if (_cache.TryGetValue(
                    cacheKey,
                    out ScholarSearchResponse? cached) &&
                cached != null)
            {
                return cached;
            }

            try
            {
                var fields = Uri.EscapeDataString(AuthorSearchFields);
                var q = Uri.EscapeDataString(normalizedQuery);
                var requestUri =
                    $"author/search?query={q}&fields={fields}&limit={effectiveLimit}";

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var fetchedAt = DateTime.UtcNow;

                if ((int)response.StatusCode == 429)
                {
                    var failure = new ScholarSearchResponse
                    {
                        Query = normalizedQuery,
                        LookupStatus = RateLimited,
                        SourceFetchedAt = fetchedAt,
                        Message = "Semantic Scholar author search is temporarily rate limited.",
                        RetryAfterSeconds = GetRetryAfterSeconds(response)
                    };
                    return failure;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning(
                        "Semantic Scholar author search failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (response.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        return new ScholarSearchResponse
                        {
                            Query = normalizedQuery,
                            LookupStatus = ProviderTimeout,
                            SourceFetchedAt = fetchedAt,
                            Message = "Semantic Scholar author search timed out."
                        };
                    }

                    if (statusCode >= 500)
                    {
                        return new ScholarSearchResponse
                        {
                            Query = normalizedQuery,
                            LookupStatus = ProviderUnavailable,
                            SourceFetchedAt = fetchedAt,
                            Message = "Semantic Scholar is temporarily unavailable."
                        };
                    }

                    return new ScholarSearchResponse
                    {
                        Query = normalizedQuery,
                        LookupStatus = ProviderError,
                        SourceFetchedAt = fetchedAt,
                        Message = "Semantic Scholar returned an unexpected response."
                    };
                }

                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                var payload = await JsonSerializer
                    .DeserializeAsync<ScholarAuthorSearchApiResponse>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                if (payload == null)
                {
                    return new ScholarSearchResponse
                    {
                        Query = normalizedQuery,
                        LookupStatus = ProviderError,
                        SourceFetchedAt = fetchedAt,
                        Message = "Semantic Scholar returned an invalid search response."
                    };
                }

                var result = new ScholarSearchResponse
                {
                    Query = normalizedQuery,
                    LookupStatus = Found,
                    SourceFetchedAt = fetchedAt,
                    Total = payload.Total ?? 0,
                    Offset = payload.Offset ?? 0,
                    Next = payload.Next,
                    Data = (payload.Data ?? new List<ScholarAuthorSummaryApiResponse>())
                        .Select(MapAuthorSummary)
                        .ToList()
                };

                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(
                        Math.Clamp(_settings.AuthorCacheSeconds, 30, 86400)));

                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Semantic Scholar author search timed out.");

                return new ScholarSearchResponse
                {
                    Query = normalizedQuery,
                    LookupStatus = ProviderTimeout,
                    SourceFetchedAt = DateTime.UtcNow,
                    Message = "Semantic Scholar author search timed out."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar author search failed because of a network or transport error.");

                return new ScholarSearchResponse
                {
                    Query = normalizedQuery,
                    LookupStatus = ProviderUnavailable,
                    SourceFetchedAt = DateTime.UtcNow,
                    Message = "Semantic Scholar is temporarily unavailable."
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar author search returned malformed JSON.");

                return new ScholarSearchResponse
                {
                    Query = normalizedQuery,
                    LookupStatus = ProviderError,
                    SourceFetchedAt = DateTime.UtcNow,
                    Message = "Semantic Scholar returned an invalid response."
                };
            }
        }

        public async Task<ScholarAuthorPapersResponse> GetAuthorPapersAsync(
            string authorId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidAuthorId(authorId, out var normalizedAuthorId))
            {
                return PapersFailure(
                    authorId?.Trim() ?? string.Empty,
                    InvalidAuthorId,
                    "The supplied Semantic Scholar Author ID is invalid.");
            }

            var effectiveLimit = Math.Clamp(limit, 1, _settings.MaxPapers);

            var cacheKey =
                $"scholar:author-papers:{normalizedAuthorId}:{effectiveLimit}";

            if (_cache.TryGetValue(
                    cacheKey,
                    out ScholarAuthorPapersResponse? cached) &&
                cached != null)
            {
                return cached;
            }

            try
            {
                var fields = Uri.EscapeDataString(PaperFields);
                var requestUri =
                    $"author/{normalizedAuthorId}/papers?fields={fields}&limit={effectiveLimit}";

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var fetchedAt = DateTime.UtcNow;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return PapersFailure(
                        normalizedAuthorId,
                        NotFound,
                        "No Semantic Scholar author or papers were found for this Author ID.",
                        fetchedAt);
                }

                if ((int)response.StatusCode == 429)
                {
                    return PapersFailure(
                        normalizedAuthorId,
                        RateLimited,
                        "Semantic Scholar papers lookup is temporarily rate limited.",
                        fetchedAt,
                        GetRetryAfterSeconds(response));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning(
                        "Semantic Scholar papers lookup failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (response.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        return PapersFailure(
                            normalizedAuthorId,
                            ProviderTimeout,
                            "Semantic Scholar papers lookup timed out.",
                            fetchedAt);
                    }

                    if (statusCode >= 500)
                    {
                        return PapersFailure(
                            normalizedAuthorId,
                            ProviderUnavailable,
                            "Semantic Scholar is temporarily unavailable.",
                            fetchedAt);
                    }

                    return PapersFailure(
                        normalizedAuthorId,
                        ProviderError,
                        "Semantic Scholar returned an unexpected response.",
                        fetchedAt);
                }

                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                var payload = await JsonSerializer
                    .DeserializeAsync<ScholarPaperListApiResponse>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                if (payload == null)
                {
                    return PapersFailure(
                        normalizedAuthorId,
                        ProviderError,
                        "Semantic Scholar returned an invalid papers response.",
                        fetchedAt);
                }

                var result = new ScholarAuthorPapersResponse
                {
                    AuthorId = normalizedAuthorId,
                    LookupStatus = Found,
                    SourceFetchedAt = fetchedAt,
                    Offset = payload.Offset ?? 0,
                    Next = payload.Next,
                    Data = (payload.Data ?? new List<ScholarPaperApiResponse>())
                        .Select(MapPaper)
                        .ToList()
                };

                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(
                        Math.Clamp(_settings.PaperCacheSeconds, 30, 86400)));

                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Semantic Scholar papers lookup timed out.");

                return PapersFailure(
                    normalizedAuthorId,
                    ProviderTimeout,
                    "Semantic Scholar papers lookup timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar papers lookup failed because of a network or transport error.");

                return PapersFailure(
                    normalizedAuthorId,
                    ProviderUnavailable,
                    "Semantic Scholar is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Semantic Scholar papers lookup returned malformed JSON.");

                return PapersFailure(
                    normalizedAuthorId,
                    ProviderError,
                    "Semantic Scholar returned an invalid papers response.");
            }
        }

        // ===== Mapping helpers =====

        private static ScholarAuthorProfileResponse MapAuthorProfile(
            ScholarAuthorApiResponse author,
            DateTime fetchedAt)
        {
            var externalUrl = !string.IsNullOrWhiteSpace(author.AuthorId)
                ? $"https://www.semanticscholar.org/author/{author.AuthorId}"
                : author.Homepage;

            return new ScholarAuthorProfileResponse
            {
                AuthorId = author.AuthorId ?? string.Empty,
                LookupStatus = Found,
                SourceFetchedAt = fetchedAt,
                Name = author.Name,
                Affiliations = author.Affiliations ?? new List<string>(),
                Homepage = author.Homepage,
                PaperCount = author.PaperCount ?? 0,
                CitationCount = author.CitationCount ?? 0,
                HIndex = author.HIndex,
                ExternalUrl = externalUrl
            };
        }

        private static ScholarAuthorSummaryResponse MapAuthorSummary(
            ScholarAuthorSummaryApiResponse author)
        {
            return new ScholarAuthorSummaryResponse
            {
                AuthorId = author.AuthorId ?? string.Empty,
                Name = author.Name,
                Affiliations = author.Affiliations ?? new List<string>(),
                PaperCount = author.PaperCount,
                CitationCount = author.CitationCount,
                HIndex = author.HIndex
            };
        }

        private static ScholarPaperResponse MapPaper(
            ScholarPaperApiResponse paper)
        {
            string? doi = null;
            string? arxivId = null;
            string? externalIdsJson = null;

            if (paper.ExternalIds != null)
            {
                doi = paper.ExternalIds.DOI;
                arxivId = paper.ExternalIds.ArXiv;

                try
                {
                    externalIdsJson = JsonSerializer.Serialize(
                        paper.ExternalIds,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition =
                                JsonIgnoreCondition.WhenWritingNull
                        });
                }
                catch
                {
                    externalIdsJson = null;
                }
            }

            string? externalUrl = null;

            if (!string.IsNullOrWhiteSpace(paper.PaperId))
            {
                externalUrl = paper.PaperId.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase)
                    ? paper.PaperId
                    : $"https://www.semanticscholar.org/paper/{paper.PaperId}";
            }
            else if (!string.IsNullOrWhiteSpace(doi))
            {
                externalUrl = doi.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase)
                    ? doi
                    : $"https://doi.org/{doi}";
            }

            return new ScholarPaperResponse
            {
                PaperId = paper.PaperId,
                Title = paper.Title,
                Abstract = paper.Abstract,
                Year = paper.Year,
                Venue = paper.Venue,
                CitationCount = paper.CitationCount,
                ReferenceCount = paper.ReferenceCount,
                InfluentialCitationCount = paper.InfluentialCitationCount,
                Doi = doi,
                ArxivId = arxivId,
                ExternalIds = externalIdsJson,
                ExternalUrl = externalUrl,
                Authors = (paper.Authors ?? new List<ScholarPaperAuthorApiResponse>())
                    .Select(MapPaperAuthor)
                    .ToList()
            };
        }

        private static ScholarPaperAuthorRef MapPaperAuthor(
            ScholarPaperAuthorApiResponse author)
        {
            return new ScholarPaperAuthorRef
            {
                AuthorId = author.AuthorId,
                Name = author.Name
            };
        }

        // ===== Validation helpers =====

        private static bool IsValidAuthorId(
            string? input,
            out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var value = input.Trim();

            // Author ID của Semantic Scholar là chuỗi hex/chữ-số, vd "1741101" hoặc dạng UUID
            // Cho phép chữ, số, dấu gạch ngang, dấu chấm, dấu gạch dưới. Độ dài 1-100.
            if (value.Length > 100)
            {
                return false;
            }

            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch) &&
                    ch != '-' &&
                    ch != '_' &&
                    ch != '.')
                {
                    return false;
                }
            }

            normalized = value;
            return true;
        }

        private static int? GetRetryAfterSeconds(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;

            if (retryAfter?.Delta is TimeSpan delta)
            {
                return Math.Max(0, (int)Math.Ceiling(delta.TotalSeconds));
            }

            if (retryAfter?.Date is DateTimeOffset retryDate)
            {
                var seconds = (retryDate - DateTimeOffset.UtcNow).TotalSeconds;
                return Math.Max(0, (int)Math.Ceiling(seconds));
            }

            // Semantic Scholar không trả X-RateLimit-Reset nhưng ta fallback an toàn về 5s
            return 5;
        }

        // ===== Failure builders =====

        private static ScholarAuthorProfileResponse ProfileFailure(
            string authorId,
            string lookupStatus,
            string message,
            DateTime? fetchedAt = null,
            int? retryAfterSeconds = null)
        {
            return new ScholarAuthorProfileResponse
            {
                AuthorId = authorId,
                LookupStatus = lookupStatus,
                SourceFetchedAt = fetchedAt ?? DateTime.UtcNow,
                Message = message,
                RetryAfterSeconds = retryAfterSeconds
            };
        }

        private static ScholarAuthorPapersResponse PapersFailure(
            string authorId,
            string lookupStatus,
            string message,
            DateTime? fetchedAt = null,
            int? retryAfterSeconds = null)
        {
            return new ScholarAuthorPapersResponse
            {
                AuthorId = authorId,
                LookupStatus = lookupStatus,
                SourceFetchedAt = fetchedAt ?? DateTime.UtcNow,
                Message = message,
                RetryAfterSeconds = retryAfterSeconds
            };
        }

        // ===== Private API response classes =====

        private sealed class ScholarAuthorApiResponse
        {
            [JsonPropertyName("authorId")]
            public string? AuthorId { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("affiliations")]
            public List<string>? Affiliations { get; set; }

            [JsonPropertyName("homepage")]
            public string? Homepage { get; set; }

            [JsonPropertyName("paperCount")]
            public int? PaperCount { get; set; }

            [JsonPropertyName("citationCount")]
            public int? CitationCount { get; set; }

            [JsonPropertyName("hIndex")]
            public int? HIndex { get; set; }
        }

        private sealed class ScholarAuthorSearchApiResponse
        {
            [JsonPropertyName("total")]
            public int? Total { get; set; }

            [JsonPropertyName("offset")]
            public int? Offset { get; set; }

            [JsonPropertyName("next")]
            public int? Next { get; set; }

            [JsonPropertyName("data")]
            public List<ScholarAuthorSummaryApiResponse>? Data { get; set; }
        }

        private sealed class ScholarAuthorSummaryApiResponse
        {
            [JsonPropertyName("authorId")]
            public string? AuthorId { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("affiliations")]
            public List<string>? Affiliations { get; set; }

            [JsonPropertyName("paperCount")]
            public int? PaperCount { get; set; }

            [JsonPropertyName("citationCount")]
            public int? CitationCount { get; set; }

            [JsonPropertyName("hIndex")]
            public int? HIndex { get; set; }
        }

        private sealed class ScholarPaperListApiResponse
        {
            [JsonPropertyName("offset")]
            public int? Offset { get; set; }

            [JsonPropertyName("next")]
            public int? Next { get; set; }

            [JsonPropertyName("data")]
            public List<ScholarPaperApiResponse>? Data { get; set; }
        }

        private sealed class ScholarPaperApiResponse
        {
            [JsonPropertyName("paperId")]
            public string? PaperId { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("abstract")]
            public string? Abstract { get; set; }

            [JsonPropertyName("year")]
            public int? Year { get; set; }

            [JsonPropertyName("venue")]
            public string? Venue { get; set; }

            [JsonPropertyName("citationCount")]
            public int? CitationCount { get; set; }

            [JsonPropertyName("referenceCount")]
            public int? ReferenceCount { get; set; }

            [JsonPropertyName("influentialCitationCount")]
            public int? InfluentialCitationCount { get; set; }

            [JsonPropertyName("externalIds")]
            public ScholarPaperExternalIdsApiResponse? ExternalIds { get; set; }

            [JsonPropertyName("authors")]
            public List<ScholarPaperAuthorApiResponse>? Authors { get; set; }
        }

        private sealed class ScholarPaperExternalIdsApiResponse
        {
            [JsonPropertyName("DOI")]
            public string? DOI { get; set; }

            [JsonPropertyName("ArXiv")]
            public string? ArXiv { get; set; }

            [JsonPropertyName("MAG")]
            public string? MAG { get; set; }

            [JsonPropertyName("PMID")]
            public string? PMID { get; set; }

            [JsonPropertyName("PMCID")]
            public string? PMCID { get; set; }
        }

        private sealed class ScholarPaperAuthorApiResponse
        {
            [JsonPropertyName("authorId")]
            public string? AuthorId { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}
