using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
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
        private const string InvalidWorkId = "InvalidWorkId";
        private const string NotFound = "NotFound";
        private const string RateLimited = "RateLimited";
        private const string ProviderUnavailable = "ProviderUnavailable";
        private const string ProviderTimeout = "ProviderTimeout";
        private const string ProviderError = "ProviderError";

        private const string WorksSelect =
            "id,doi,title,display_name,publication_year,publication_date,type," +
            "cited_by_count,is_retracted,primary_location,open_access";

        private const string WorkLookupSelect =
            "id,doi,title,display_name,publication_year,publication_date,type," +
            "cited_by_count,is_retracted,primary_location,open_access,authorships";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly OpenAlexSettings _settings;
        private readonly ILogger<OpenAlexService> _logger;
        private readonly IMemoryCache _cache;

        public OpenAlexService(
            HttpClient httpClient,
            IOptions<OpenAlexSettings> settings,
            ILogger<OpenAlexService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _cache = cache;
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

        public async Task<OpenAlexWorkLookupResponse> LookupWorkByIdAsync(
            string openAlexWorkId,
            CancellationToken cancellationToken = default)
        {
            if (!TryNormalizeOpenAlexWorkId(
                    openAlexWorkId,
                    out var normalizedWorkId))
            {
                return WorkFailure(
                    openAlexWorkId?.Trim() ?? string.Empty,
                    InvalidWorkId,
                    "The supplied OpenAlex Work ID is invalid.");
            }

            try
            {
                var select =
                    Uri.EscapeDataString(
                        WorkLookupSelect);

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        $"works/{normalizedWorkId}?select={select}");

                using var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                var fetchedAt =
                    DateTime.UtcNow;

                if (response.StatusCode ==
                    HttpStatusCode.NotFound)
                {
                    return WorkFailure(
                        normalizedWorkId,
                        NotFound,
                        "No OpenAlex work was found for this Work ID.",
                        fetchedAt);
                }

                if ((int)response.StatusCode == 429)
                {
                    return WorkFailure(
                        normalizedWorkId,
                        RateLimited,
                        "OpenAlex work lookup is temporarily rate limited.",
                        fetchedAt,
                        GetRetryAfterSeconds(response));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode =
                        (int)response.StatusCode;

                    _logger.LogWarning(
                        "OpenAlex work lookup failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (statusCode >= 500 ||
                        response.StatusCode ==
                        HttpStatusCode.RequestTimeout)
                    {
                        return WorkFailure(
                            normalizedWorkId,
                            ProviderUnavailable,
                            "OpenAlex is temporarily unavailable.",
                            fetchedAt);
                    }

                    return WorkFailure(
                        normalizedWorkId,
                        ProviderError,
                        "OpenAlex returned an unexpected response.",
                        fetchedAt);
                }

                await using var stream =
                    await response.Content
                        .ReadAsStreamAsync(
                            cancellationToken);

                var work =
                    await JsonSerializer
                        .DeserializeAsync<OpenAlexWorkApiResponse>(
                            stream,
                            JsonOptions,
                            cancellationToken);

                if (work == null ||
                    string.IsNullOrWhiteSpace(work.Id))
                {
                    _logger.LogWarning(
                        "OpenAlex work lookup returned a successful response without a work ID.");

                    return WorkFailure(
                        normalizedWorkId,
                        ProviderError,
                        "OpenAlex returned incomplete work metadata.",
                        fetchedAt);
                }

                var authorships =
                    (work.Authorships
                        ?? new List<OpenAlexAuthorshipApiResponse>())
                    .Select(authorship =>
                        new OpenAlexWorkAuthorshipResponse
                        {
                            RawAuthorName =
                                authorship.RawAuthorName,

                            RawOrcid =
                                authorship.RawOrcid,

                            AuthorOpenAlexId =
                                authorship.Author?.Id,

                            AuthorDisplayName =
                                authorship.Author?.DisplayName,

                            AuthorOrcid =
                                authorship.Author?.Orcid,

                            IsCorresponding =
                                authorship.IsCorresponding
                        })
                    .ToList();

                return new OpenAlexWorkLookupResponse
                {
                    OpenAlexWorkId =
                        normalizedWorkId,

                    LookupStatus =
                        Found,

                    SourceFetchedAt =
                        fetchedAt,

                    Work =
                        MapWork(work),

                    Authorships =
                        authorships
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "OpenAlex work lookup timed out.");

                return WorkFailure(
                    normalizedWorkId,
                    ProviderUnavailable,
                    "OpenAlex work lookup timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex work lookup failed because of a network or transport error.");

                return WorkFailure(
                    normalizedWorkId,
                    ProviderUnavailable,
                    "OpenAlex is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex work lookup returned malformed JSON.");

                return WorkFailure(
                    normalizedWorkId,
                    ProviderError,
                    "OpenAlex returned an invalid work response.");
            }
        }

        public async Task<OpenAlexWorkPreviewResponse> GetWorkPreviewByIdAsync(
            string workId,
            CancellationToken cancellationToken = default)
        {
            if (!IsCanonicalOpenAlexWorkId(workId))
            {
                return PreviewFailure(
                    workId?.Trim() ?? string.Empty,
                    InvalidWorkId,
                    "The supplied OpenAlex Work ID must be a canonical W-prefixed ID.");
            }

            var normalizedWorkId = workId.Trim();
            var cacheKey = $"openalex:work-preview:{normalizedWorkId}";

            if (_cache.TryGetValue(
                    cacheKey,
                    out OpenAlexWorkPreviewResponse? cached) &&
                cached != null)
            {
                return cached;
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"works/{normalizedWorkId}");

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var fetchedAt = DateTime.UtcNow;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return PreviewFailure(
                        normalizedWorkId,
                        NotFound,
                        "No OpenAlex work was found for this Work ID.",
                        fetchedAt);
                }

                if ((int)response.StatusCode == 429)
                {
                    return PreviewFailure(
                        normalizedWorkId,
                        RateLimited,
                        "OpenAlex work lookup is temporarily rate limited.",
                        fetchedAt,
                        GetRetryAfterSeconds(response));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;

                    _logger.LogWarning(
                        "OpenAlex work preview lookup failed with HTTP status {StatusCode}.",
                        statusCode);

                    if (response.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        return PreviewFailure(
                            normalizedWorkId,
                            ProviderTimeout,
                            "OpenAlex work lookup timed out.",
                            fetchedAt);
                    }

                    if (statusCode >= 500)
                    {
                        return PreviewFailure(
                            normalizedWorkId,
                            ProviderUnavailable,
                            "OpenAlex is temporarily unavailable.",
                            fetchedAt);
                    }

                    return PreviewFailure(
                        normalizedWorkId,
                        ProviderError,
                        "OpenAlex returned an unexpected response.",
                        fetchedAt);
                }

                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                var work = await JsonSerializer
                    .DeserializeAsync<OpenAlexWorkApiResponse>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                if (work == null ||
                    string.IsNullOrWhiteSpace(work.Id))
                {
                    _logger.LogWarning(
                        "OpenAlex work preview lookup returned a successful response without a work ID.");

                    return PreviewFailure(
                        normalizedWorkId,
                        ProviderError,
                        "OpenAlex returned incomplete work metadata.",
                        fetchedAt);
                }

                var result = MapWorkPreview(
                    normalizedWorkId,
                    fetchedAt,
                    work);

                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(
                        Math.Clamp(
                            _settings.WorkCacheSeconds,
                            30,
                            3600)));

                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "OpenAlex work preview lookup timed out.");

                return PreviewFailure(
                    normalizedWorkId,
                    ProviderTimeout,
                    "OpenAlex work lookup timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex work preview lookup failed because of a network or transport error.");

                return PreviewFailure(
                    normalizedWorkId,
                    ProviderUnavailable,
                    "OpenAlex is temporarily unavailable.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenAlex work preview lookup returned malformed JSON.");

                return PreviewFailure(
                    normalizedWorkId,
                    ProviderError,
                    "OpenAlex returned an invalid work response.");
            }
        }

        private static OpenAlexWorkPreviewResponse MapWorkPreview(
            string normalizedWorkId,
            DateTime fetchedAt,
            OpenAlexWorkApiResponse work)
        {
            var authors = (work.Authorships ?? new List<OpenAlexAuthorshipApiResponse>())
                .Select(authorship => new OpenAlexWorkPreviewAuthorResponse
                {
                    RawAuthorName = authorship.RawAuthorName,
                    RawOrcid = NormalizeOrcidForResponse(authorship.RawOrcid),
                    AuthorOpenAlexId = authorship.Author?.Id,
                    AuthorDisplayName = authorship.Author?.DisplayName,
                    AuthorOrcid = NormalizeOrcidForResponse(authorship.Author?.Orcid),
                    IsCorresponding = authorship.IsCorresponding,
                    Institutions = (authorship.Institutions ?? new List<OpenAlexInstitutionApiResponse>())
                        .Select(institution => new OpenAlexWorkPreviewInstitutionResponse
                        {
                            OpenAlexId = institution.Id,
                            DisplayName = institution.DisplayName,
                            Ror = institution.Ror,
                            CountryCode = institution.CountryCode,
                            Type = institution.Type
                        })
                        .ToList()
                })
                .ToList();

            var topics = (work.Topics ?? new List<OpenAlexTopicApiResponse>())
                .Select(topic => new OpenAlexWorkPreviewTopicResponse
                {
                    TopicId = topic.Id,
                    TopicName = topic.DisplayName,
                    Score = topic.Score,
                    SubFieldId = topic.SubField?.Id,
                    SubFieldName = topic.SubField?.DisplayName,
                    FieldId = topic.Field?.Id,
                    FieldName = topic.Field?.DisplayName,
                    DomainId = topic.Domain?.Id,
                    DomainName = topic.Domain?.DisplayName
                })
                .ToList();

            var concepts = (work.Concepts ?? new List<OpenAlexConceptApiResponse>())
                .Select(concept => new OpenAlexWorkPreviewConceptResponse
                {
                    ConceptId = concept.Id,
                    ConceptName = concept.DisplayName,
                    Score = concept.Score,
                    Level = concept.Level
                })
                .ToList();

            OpenAlexWorkPreviewSourceResponse? source = null;
            var sourceApi = work.PrimaryLocation?.Source;

            if (sourceApi != null)
            {
                source = new OpenAlexWorkPreviewSourceResponse
                {
                    OpenAlexSourceId = sourceApi.Id,
                    DisplayName = sourceApi.DisplayName,
                    IssnL = sourceApi.IssnL,
                    Issns = sourceApi.Issn ?? new List<string>(),
                    Type = sourceApi.Type,
                    HostOrganizationOpenAlexId = sourceApi.HostOrganization
                };
            }

            return new OpenAlexWorkPreviewResponse
            {
                OpenAlexWorkId = normalizedWorkId,
                LookupStatus = Found,
                SourceFetchedAt = fetchedAt,
                Title = work.Title ?? work.DisplayName,
                Abstract = ReconstructAbstract(work.AbstractInvertedIndex),
                PublicationYear = work.PublicationYear,
                PublicationDate = ParseDate(work.PublicationDate),
                Doi = work.Doi,
                Type = work.Type,
                CitedByCount = work.CitedByCount ?? 0,
                IsRetracted = work.IsRetracted ?? false,
                IsOpenAccess = work.OpenAccess?.IsOpenAccess,
                OpenAccessStatus = work.OpenAccess?.Status,
                Authors = authors,
                Topics = topics,
                Concepts = concepts,
                Source = source,
                ExternalUrl = !string.IsNullOrWhiteSpace(work.Id)
                    ? work.Id
                    : work.Doi
            };
        }

        private static string? NormalizeOrcidForResponse(
            string? orcid)
        {
            return OrcidIdUtility.TryNormalizeAndValidate(
                    orcid,
                    out var normalized)
                ? normalized
                : null;
        }

        private static string? ReconstructAbstract(
            Dictionary<string, List<int>>? invertedIndex)
        {
            if (invertedIndex == null || invertedIndex.Count == 0)
            {
                return null;
            }

            var words = new SortedDictionary<int, string>();

            foreach (var entry in invertedIndex)
            {
                foreach (var position in entry.Value)
                {
                    words[position] = entry.Key;
                }
            }

            return words.Count == 0
                ? null
                : string.Join(" ", words.Values);
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

        private static bool IsCanonicalOpenAlexWorkId(
            string? input)
        {
            if (string.IsNullOrWhiteSpace(input) ||
                input.Length < 2 ||
                input[0] != 'W')
            {
                return false;
            }

            for (var index = 1; index < input.Length; index++)
            {
                if (!char.IsDigit(input[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryNormalizeOpenAlexWorkId(
            string? input,
            out string normalizedWorkId)
        {
            normalizedWorkId =
                string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var value =
                input.Trim();

            if (Uri.TryCreate(
                    value,
                    UriKind.Absolute,
                    out var uri))
            {
                if (string.Equals(
                        uri.Host,
                        "openalex.org",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(uri.Query) ||
                        !string.IsNullOrEmpty(uri.Fragment))
                    {
                        return false;
                    }

                    value =
                        uri.AbsolutePath.Trim('/');
                }
                else if (string.Equals(
                             uri.Host,
                             "api.openalex.org",
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(uri.Query) ||
                        !string.IsNullOrEmpty(uri.Fragment))
                    {
                        return false;
                    }

                    var segments =
                        uri.AbsolutePath
                            .Trim('/')
                            .Split(
                                '/',
                                StringSplitOptions.RemoveEmptyEntries);

                    if (segments.Length != 2 ||
                        !string.Equals(
                            segments[0],
                            "works",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    value =
                        segments[1];
                }
                else
                {
                    return false;
                }
            }

            if (value.StartsWith(
                    "works/",
                    StringComparison.OrdinalIgnoreCase))
            {
                value =
                    value["works/".Length..];
            }

            value =
                value
                    .Trim()
                    .Trim('/')
                    .ToUpperInvariant();

            if (value.Length < 2 ||
                value[0] != 'W')
            {
                return false;
            }

            for (var index = 1;
                 index < value.Length;
                 index++)
            {
                if (!char.IsDigit(
                        value[index]))
                {
                    return false;
                }
            }

            normalizedWorkId =
                value;

            return true;
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

        private static OpenAlexWorkPreviewResponse PreviewFailure(
            string openAlexWorkId,
            string lookupStatus,
            string message,
            DateTime? fetchedAt = null,
            int? retryAfterSeconds = null)
        {
            return new OpenAlexWorkPreviewResponse
            {
                OpenAlexWorkId = openAlexWorkId,
                LookupStatus = lookupStatus,
                SourceFetchedAt = fetchedAt ?? DateTime.UtcNow,
                Message = message,
                RetryAfterSeconds = retryAfterSeconds
            };
        }

        private static OpenAlexWorkLookupResponse WorkFailure(
            string openAlexWorkId,
            string lookupStatus,
            string message,
            DateTime? fetchedAt = null,
            int? retryAfterSeconds = null)
        {
            return new OpenAlexWorkLookupResponse
            {
                OpenAlexWorkId =
                    openAlexWorkId,

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

            [JsonPropertyName("score")]
            public double? Score { get; set; }

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

            [JsonPropertyName("abstract_inverted_index")]
            public Dictionary<string, List<int>>? AbstractInvertedIndex { get; set; }

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

            [JsonPropertyName("authorships")]
            public List<OpenAlexAuthorshipApiResponse>?
                Authorships
            { get; set; }

            [JsonPropertyName("topics")]
            public List<OpenAlexTopicApiResponse>?
                Topics
            { get; set; }

            [JsonPropertyName("concepts")]
            public List<OpenAlexConceptApiResponse>?
                Concepts
            { get; set; }
        }

        private sealed class OpenAlexAuthorshipApiResponse
        {
            [JsonPropertyName("raw_author_name")]
            public string? RawAuthorName { get; set; }

            [JsonPropertyName("raw_orcid")]
            public string? RawOrcid { get; set; }

            [JsonPropertyName("is_corresponding")]
            public bool? IsCorresponding { get; set; }

            [JsonPropertyName("author")]
            public OpenAlexAuthorshipAuthorApiResponse?
                Author
            { get; set; }

            [JsonPropertyName("institutions")]
            public List<OpenAlexInstitutionApiResponse>?
                Institutions
            { get; set; }
        }

        private sealed class OpenAlexAuthorshipAuthorApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("orcid")]
            public string? Orcid { get; set; }
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
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("issn_l")]
            public string? IssnL { get; set; }

            [JsonPropertyName("issn")]
            public List<string>? Issn { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("host_organization")]
            public string? HostOrganization { get; set; }
        }

        private sealed class OpenAlexConceptApiResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("level")]
            public int? Level { get; set; }

            [JsonPropertyName("score")]
            public double? Score { get; set; }
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