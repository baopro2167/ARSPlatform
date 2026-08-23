using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class OpenAlexService : IOpenAlexService
    {
        private const string Found = "Found";
        private const string InvalidOrcid = "InvalidOrcid";
        private const string NotFound = "NotFound";
        private const string RateLimited = "RateLimited";
        private const string ProviderUnavailable = "ProviderUnavailable";
        private const string ProviderError = "ProviderError";

        private const string WorksSelect =
            "id,doi,title,display_name,publication_year,publication_date,type," +
            "cited_by_count,is_retracted,primary_location,open_access";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly OpenAlexSettings _settings;
        private readonly ILogger<OpenAlexService> _logger;

        public OpenAlexService(
            HttpClient httpClient,
            IOptions<OpenAlexSettings> settings,
            ILogger<OpenAlexService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<OrcidLookupResponse> LookupByOrcidAsync(
            string orcidId,
            CancellationToken cancellationToken = default)
        {
            if (!OrcidIdUtility.TryNormalizeAndValidate(
                    orcidId,
                    out var normalizedOrcidId))
            {
                return Failure(
                    orcidId?.Trim() ?? string.Empty,
                    InvalidOrcid,
                    "The supplied ORCID iD is invalid.");
            }

            try
            {
                var orcidUrl =
    OrcidIdUtility.ToHttpsUrl(
        normalizedOrcidId);

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"authors/{orcidUrl}");

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var fetchedAt = DateTime.UtcNow;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return Failure(
                        normalizedOrcidId,
                        NotFound,
                        "No OpenAlex author was found for this ORCID iD.",
                        fetchedAt);
                }

                if ((int)response.StatusCode == 429)
                {
                    return Failure(
                        normalizedOrcidId,
                        RateLimited,
                        "OpenAlex metadata lookup is temporarily rate limited.",
                        fetchedAt,
                        GetRetryAfterSeconds(response));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;

                    _logger.LogWarning(
                        "OpenAlex author lookup failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (statusCode >= 500 ||
                        response.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        return Failure(
                            normalizedOrcidId,
                            ProviderUnavailable,
                            "OpenAlex is temporarily unavailable.",
                            fetchedAt);
                    }

                    return Failure(
                        normalizedOrcidId,
                        ProviderError,
                        "OpenAlex returned an unexpected response.",
                        fetchedAt);
                }

                await using var stream =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                var author =
                    await JsonSerializer
                        .DeserializeAsync<OpenAlexAuthorApiResponse>(
                            stream,
                            JsonOptions,
                            cancellationToken);

                if (author == null ||
                    string.IsNullOrWhiteSpace(author.Id))
                {
                    _logger.LogWarning(
                        "OpenAlex author lookup returned a successful response without an author ID.");

                    return Failure(
                        normalizedOrcidId,
                        ProviderError,
                        "OpenAlex returned incomplete author metadata.",
                        fetchedAt);
                }

                var result = MapAuthor(
                    normalizedOrcidId,
                    fetchedAt,
                    author);

                await PopulateWorksAsync(
                    result,
                    author.Id,
                    cancellationToken);

                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "OpenAlex author lookup timed out.");

                return Failure(
                    normalizedOrcidId,
                    ProviderUnavailable,
                    "OpenAlex metadata lookup timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex author lookup failed because of a network or transport error.");

                return Failure(
                    normalizedOrcidId,
                    ProviderUnavailable,
                    "OpenAlex is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex author lookup returned malformed JSON.");

                return Failure(
                    normalizedOrcidId,
                    ProviderError,
                    "OpenAlex returned an invalid response.");
            }
        }

        private OrcidLookupResponse MapAuthor(
            string normalizedOrcidId,
            DateTime fetchedAt,
            OpenAlexAuthorApiResponse author)
        {
            var result = new OrcidLookupResponse
            {
                OrcidId = normalizedOrcidId,
                LookupStatus = Found,
                SourceFetchedAt = fetchedAt,

                Author = new OpenAlexAuthorResponse
                {
                    OpenAlexId = author.Id,

                    Orcid =
                        author.Orcid
                        ?? OrcidIdUtility.ToHttpsUrl(
                            normalizedOrcidId),

                    DisplayName = author.DisplayName,

                    FullName = author.FullName,

                    AlternativeNames =
                        author.DisplayNameAlternatives
                        ?? new List<string>(),

                    RawAuthorNames =
                        author.RawAuthorNames
                        ?? new List<string>(),

                    ExternalUrl = author.Id
                },

                Metrics = new OpenAlexMetricsResponse
                {
                    WorksCount =
                        author.WorksCount
                        ?? 0,

                    CitedByCount =
                        author.CitedByCount
                        ?? 0,

                    HIndex =
                        author.SummaryStats?.HIndex,

                    I10Index =
                        author.SummaryStats?.I10Index,

                    TwoYearMeanCitedness =
                        author.SummaryStats
                            ?.TwoYearMeanCitedness
                },

                Affiliations =
                    MapAffiliations(
                        author.Affiliations),

                LastKnownInstitutions =
                    MapInstitutions(
                        author.LastKnownInstitutions),

                Topics =
                    MapTopics(
                        author.Topics),

                CountsByYear =
                    MapCountsByYear(
                        author.CountsByYear)
            };

            if (string.IsNullOrWhiteSpace(
                    author.DisplayName))
            {
                AddMissing(
                    result,
                    "author.displayName");
            }

            if (!author.WorksCount.HasValue ||
                !author.CitedByCount.HasValue)
            {
                AddMissing(
                    result,
                    "metrics.counts");
            }

            if (author.SummaryStats == null)
            {
                AddMissing(
                    result,
                    "metrics.summaryStats");
            }

            if (result.Affiliations.Count == 0)
            {
                AddMissing(
                    result,
                    "affiliations");
            }

            if (result.LastKnownInstitutions.Count == 0)
            {
                AddMissing(
                    result,
                    "lastKnownInstitutions");
            }

            if (result.Topics.Count == 0)
            {
                AddMissing(
                    result,
                    "topics");
            }

            if (result.CountsByYear.Count == 0)
            {
                AddMissing(
                    result,
                    "countsByYear");
            }

            return result;
        }

        private async Task PopulateWorksAsync(
            OrcidLookupResponse result,
            string openAlexAuthorId,
            CancellationToken cancellationToken)
        {
            var authorId =
                ExtractOpenAlexAuthorId(
                    openAlexAuthorId);

            if (authorId == null)
            {
                AddMissing(
                    result,
                    "works");

                AddWarning(
                    result,
                    "OpenAlex did not return a usable author ID for the works lookup.");

                return;
            }

            var maxWorks = Math.Clamp(
                _settings.MaxWorks,
                1,
                100);

            var filter =
                Uri.EscapeDataString(
                    $"author.id:{authorId}");

            var select =
                Uri.EscapeDataString(
                    WorksSelect);

            var requestUri =
    $"works?filter={filter}" +
    $"&sort=cited_by_count:desc" +
    $"&per_page={maxWorks}" +
    $"&select={select}";

            try
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        requestUri);

                using var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if ((int)response.StatusCode == 429)
                {
                    AddMissing(
                        result,
                        "works");

                    AddWarning(
                        result,
                        "OpenAlex works lookup was rate limited; author metadata is still available.");

                    result.RetryAfterSeconds ??=
                        GetRetryAfterSeconds(
                            response);

                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "OpenAlex works lookup failed with HTTP status {StatusCode}.",
                        (int)response.StatusCode);

                    AddMissing(
                        result,
                        "works");

                    AddWarning(
                        result,
                        "OpenAlex works metadata is temporarily unavailable.");

                    return;
                }

                await using var stream =
                    await response.Content
                        .ReadAsStreamAsync(
                            cancellationToken);

                var workList =
                    await JsonSerializer
                        .DeserializeAsync<OpenAlexWorkListApiResponse>(
                            stream,
                            JsonOptions,
                            cancellationToken);

                if (workList?.Results == null)
                {
                    AddMissing(
                        result,
                        "works");

                    AddWarning(
                        result,
                        "OpenAlex returned an invalid works response.");

                    return;
                }

                result.Works =
                    workList.Results
                        .Select(MapWork)
                        .ToList();

                if (result.Works.Count == 0 &&
                    (result.Metrics?.WorksCount ?? 0) > 0)
                {
                    AddMissing(
                        result,
                        "works");

                    AddWarning(
                        result,
                        "OpenAlex reported publications for this author but did not return work details.");
                }
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                AddMissing(
                    result,
                    "works");

                AddWarning(
                    result,
                    "OpenAlex works lookup timed out; author metadata is still available.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex works lookup failed because of a network or transport error.");

                AddMissing(
                    result,
                    "works");

                AddWarning(
                    result,
                    "OpenAlex works metadata is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex works lookup returned malformed JSON.");

                AddMissing(
                    result,
                    "works");

                AddWarning(
                    result,
                    "OpenAlex returned invalid works metadata.");
            }
        }

        private static OpenAlexWorkResponse MapWork(
            OpenAlexWorkApiResponse work)
        {
            return new OpenAlexWorkResponse
            {
                OpenAlexId = work.Id,

                Title =
                    work.Title
                    ?? work.DisplayName,

                Doi = work.Doi,

                PublicationYear =
                    work.PublicationYear,

                PublicationDate =
                    ParseDate(
                        work.PublicationDate),

                Type = work.Type,

                CitedByCount =
                    work.CitedByCount
                    ?? 0,

                SourceName =
                    work.PrimaryLocation
                        ?.Source
                        ?.DisplayName,

                IsOpenAccess =
                    work.OpenAccess
                        ?.IsOpenAccess,

                OpenAccessStatus =
                    work.OpenAccess
                        ?.Status,

                IsRetracted =
                    work.IsRetracted
                    ?? false,

                ExternalUrl =
                    !string.IsNullOrWhiteSpace(
                        work.Doi)
                        ? work.Doi
                        : work.Id
            };
        }

        private static List<OpenAlexAffiliationResponse>
            MapAffiliations(
                List<OpenAlexAffiliationApiResponse>? affiliations)
        {
            if (affiliations == null)
            {
                return new List<OpenAlexAffiliationResponse>();
            }

            return affiliations
                .Where(item =>
                    item.Institution != null)
                .Select(item =>
                    new OpenAlexAffiliationResponse
                    {
                        InstitutionOpenAlexId =
                            item.Institution!.Id,

                        InstitutionName =
                            item.Institution
                                .DisplayName,

                        Ror =
                            item.Institution
                                .Ror,

                        CountryCode =
                            item.Institution
                                .CountryCode,

                        Type =
                            item.Institution
                                .Type,

                        Years =
                            (item.Years
                                ?? new List<int>())
                            .OrderByDescending(
                                year => year)
                            .ToList()
                    })
                .ToList();
        }

        private static List<OpenAlexInstitutionResponse>
            MapInstitutions(
                List<OpenAlexInstitutionApiResponse>? institutions)
        {
            if (institutions == null)
            {
                return new List<OpenAlexInstitutionResponse>();
            }

            return institutions
                .Select(item =>
                    new OpenAlexInstitutionResponse
                    {
                        OpenAlexId =
                            item.Id,

                        DisplayName =
                            item.DisplayName,

                        Ror =
                            item.Ror,

                        CountryCode =
                            item.CountryCode,

                        Type =
                            item.Type
                    })
                .ToList();
        }

        private static List<OpenAlexTopicResponse>
            MapTopics(
                List<OpenAlexTopicApiResponse>? topics)
        {
            if (topics == null)
            {
                return new List<OpenAlexTopicResponse>();
            }

            return topics
                .Select(item =>
                    new OpenAlexTopicResponse
                    {
                        TopicId =
                            item.Id,

                        TopicName =
                            item.DisplayName,

                        Count =
                            item.Count
                            ?? 0,

                        SubFieldId =
                            item.SubField
                                ?.Id,

                        SubFieldName =
                            item.SubField
                                ?.DisplayName,

                        FieldId =
                            item.Field
                                ?.Id,

                        FieldName =
                            item.Field
                                ?.DisplayName,

                        DomainId =
                            item.Domain
                                ?.Id,

                        DomainName =
                            item.Domain
                                ?.DisplayName
                    })
                .ToList();
        }

        private static List<OpenAlexYearCountResponse>
            MapCountsByYear(
                List<OpenAlexYearCountApiResponse>? counts)
        {
            if (counts == null)
            {
                return new List<OpenAlexYearCountResponse>();
            }

            return counts
                .Where(item =>
                    item.Year.HasValue)
                .Select(item =>
                    new OpenAlexYearCountResponse
                    {
                        Year =
                            item.Year!.Value,

                        WorksCount =
                            item.WorksCount
                            ?? 0,

                        OaWorksCount =
                            item.OaWorksCount
                            ?? 0,

                        CitedByCount =
                            item.CitedByCount
                            ?? 0
                    })
                .OrderByDescending(
                    item => item.Year)
                .ToList();
        }

        private static string? ExtractOpenAlexAuthorId(
            string openAlexId)
        {
            if (string.IsNullOrWhiteSpace(
                    openAlexId))
            {
                return null;
            }

            var value =
                openAlexId.Trim();

            var lastSlashIndex =
                value.LastIndexOf('/');

            if (lastSlashIndex >= 0 &&
                lastSlashIndex < value.Length - 1)
            {
                value =
                    value[
                        (lastSlashIndex + 1)..];
            }

            if (value.Length < 2 ||
                char.ToUpperInvariant(
                    value[0]) != 'A')
            {
                return null;
            }

            for (var index = 1;
                 index < value.Length;
                 index++)
            {
                if (!char.IsDigit(
                        value[index]))
                {
                    return null;
                }
            }

            return value.ToUpperInvariant();
        }

        private static DateTime? ParseDate(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var parsed)
                    ? parsed
                    : null;
        }

        private static int? GetRetryAfterSeconds(
    HttpResponseMessage response)
        {
            var retryAfter =
                response.Headers.RetryAfter;

            if (retryAfter?.Delta is TimeSpan delta)
            {
                return Math.Max(
                    0,
                    (int)Math.Ceiling(
                        delta.TotalSeconds));
            }

            if (retryAfter?.Date
                is DateTimeOffset retryDate)
            {
                var seconds =
                    (retryDate -
                     DateTimeOffset.UtcNow)
                    .TotalSeconds;

                return Math.Max(
                    0,
                    (int)Math.Ceiling(
                        seconds));
            }

            if (response.Headers.TryGetValues(
                    "X-RateLimit-Reset",
                    out var resetValues))
            {
                var resetValue =
                    resetValues.FirstOrDefault();

                if (int.TryParse(
                        resetValue,
                        out var resetSeconds))
                {
                    return Math.Max(
                        0,
                        resetSeconds);
                }
            }

            return null;
        }

        private static void AddMissing(
            OrcidLookupResponse result,
            string section)
        {
            if (!result.MissingSections
                    .Contains(section))
            {
                result.MissingSections
                    .Add(section);
            }
        }

        private static void AddWarning(
            OrcidLookupResponse result,
            string warning)
        {
            if (!result.ProviderWarnings
                    .Contains(warning))
            {
                result.ProviderWarnings
                    .Add(warning);
            }
        }

        private static OrcidLookupResponse Failure(
            string orcidId,
            string lookupStatus,
            string message,
            DateTime? fetchedAt = null,
            int? retryAfterSeconds = null)
        {
            return new OrcidLookupResponse
            {
                OrcidId = orcidId,

                LookupStatus =
                    lookupStatus,

                SourceFetchedAt =
                    fetchedAt
                    ?? DateTime.UtcNow,

                Message =
                    message,

                RetryAfterSeconds =
                    retryAfterSeconds
            };
        }

        private sealed class OpenAlexAuthorApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("orcid")]
            public string? Orcid { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("full_name")]
            public string? FullName { get; set; }

            [JsonPropertyName("display_name_alternatives")]
            public List<string>? DisplayNameAlternatives { get; set; }

            [JsonPropertyName("raw_author_names")]
            public List<string>? RawAuthorNames { get; set; }

            [JsonPropertyName("works_count")]
            public int? WorksCount { get; set; }

            [JsonPropertyName("cited_by_count")]
            public int? CitedByCount { get; set; }

            [JsonPropertyName("summary_stats")]
            public OpenAlexSummaryStatsApiResponse?
                SummaryStats
            { get; set; }

            [JsonPropertyName("affiliations")]
            public List<OpenAlexAffiliationApiResponse>?
                Affiliations
            { get; set; }

            [JsonPropertyName("last_known_institutions")]
            public List<OpenAlexInstitutionApiResponse>?
                LastKnownInstitutions
            { get; set; }

            [JsonPropertyName("topics")]
            public List<OpenAlexTopicApiResponse>?
                Topics
            { get; set; }

            [JsonPropertyName("counts_by_year")]
            public List<OpenAlexYearCountApiResponse>?
                CountsByYear
            { get; set; }
        }

        private sealed class OpenAlexSummaryStatsApiResponse
        {
            [JsonPropertyName("2yr_mean_citedness")]
            public double? TwoYearMeanCitedness { get; set; }

            [JsonPropertyName("h_index")]
            public int? HIndex { get; set; }

            [JsonPropertyName("i10_index")]
            public int? I10Index { get; set; }
        }

        private sealed class OpenAlexAffiliationApiResponse
        {
            [JsonPropertyName("institution")]
            public OpenAlexInstitutionApiResponse?
                Institution
            { get; set; }

            [JsonPropertyName("years")]
            public List<int>? Years { get; set; }
        }

        private sealed class OpenAlexInstitutionApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("ror")]
            public string? Ror { get; set; }

            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }
        }

        private sealed class OpenAlexTopicApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("count")]
            public int? Count { get; set; }

            [JsonPropertyName("subfield")]
            public OpenAlexClassificationApiResponse?
                SubField
            { get; set; }

            [JsonPropertyName("field")]
            public OpenAlexClassificationApiResponse?
                Field
            { get; set; }

            [JsonPropertyName("domain")]
            public OpenAlexClassificationApiResponse?
                Domain
            { get; set; }
        }

        private sealed class OpenAlexClassificationApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }
        }

        private sealed class OpenAlexYearCountApiResponse
        {
            [JsonPropertyName("year")]
            public int? Year { get; set; }

            [JsonPropertyName("works_count")]
            public int? WorksCount { get; set; }

            [JsonPropertyName("oa_works_count")]
            public int? OaWorksCount { get; set; }

            [JsonPropertyName("cited_by_count")]
            public int? CitedByCount { get; set; }
        }

        private sealed class OpenAlexWorkListApiResponse
        {
            [JsonPropertyName("results")]
            public List<OpenAlexWorkApiResponse>?
                Results
            { get; set; }
        }

        private sealed class OpenAlexWorkApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("doi")]
            public string? Doi { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("publication_year")]
            public int? PublicationYear { get; set; }

            [JsonPropertyName("publication_date")]
            public string? PublicationDate { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("cited_by_count")]
            public int? CitedByCount { get; set; }

            [JsonPropertyName("is_retracted")]
            public bool? IsRetracted { get; set; }

            [JsonPropertyName("primary_location")]
            public OpenAlexPrimaryLocationApiResponse?
                PrimaryLocation
            { get; set; }

            [JsonPropertyName("open_access")]
            public OpenAlexOpenAccessApiResponse?
                OpenAccess
            { get; set; }
        }

        private sealed class OpenAlexPrimaryLocationApiResponse
        {
            [JsonPropertyName("source")]
            public OpenAlexSourceApiResponse?
                Source
            { get; set; }
        }

        private sealed class OpenAlexSourceApiResponse
        {
            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }
        }

        private sealed class OpenAlexOpenAccessApiResponse
        {
            [JsonPropertyName("is_oa")]
            public bool? IsOpenAccess { get; set; }

            [JsonPropertyName("oa_status")]
            public string? Status { get; set; }
        }
    }
}