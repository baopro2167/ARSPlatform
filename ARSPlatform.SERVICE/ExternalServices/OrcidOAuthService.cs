using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class OrcidOAuthService : IOrcidOAuthService
    {
        private const string HttpClientName = "OrcidOAuth";

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OrcidSettings _settings;
        private readonly ILogger<OrcidOAuthService> _logger;

        public OrcidOAuthService(
            IHttpClientFactory httpClientFactory,
            IOptions<OrcidSettings> settings,
            ILogger<OrcidOAuthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public string BuildAuthorizationUrl(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException(
                    "OAuth state is required.",
                    nameof(state));
            }

            ValidateAuthorizationConfiguration();

            var authorizationUrl =
                _settings.AuthorizationUrl.Trim();

            var separator =
                authorizationUrl.Contains('?')
                    ? "&"
                    : "?";

            return authorizationUrl
                + separator
                + "client_id="
                + Uri.EscapeDataString(_settings.ClientId)
                + "&response_type=code"
                + "&scope="
                + Uri.EscapeDataString(_settings.Scope)
                + "&redirect_uri="
                + Uri.EscapeDataString(_settings.RedirectUri)
                + "&state="
                + Uri.EscapeDataString(state);
        }

        public async Task<OrcidOAuthResult> ExchangeCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Failure(
                    "INVALID_CODE",
                    "ORCID authorization code is required.");
            }

            ValidateTokenConfiguration();

            try
            {
                var client =
                    _httpClientFactory.CreateClient(
                        HttpClientName);

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        _settings.TokenUrl);

                request.Headers.Accept.ParseAdd(
                    "application/json");

                request.Content =
                    new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            ["client_id"] =
                                _settings.ClientId,

                            ["client_secret"] =
                                _settings.ClientSecret,

                            ["grant_type"] =
                                "authorization_code",

                            ["code"] =
                                code.Trim(),

                            ["redirect_uri"] =
                                _settings.RedirectUri
                        });

                using var response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                await using var stream =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                OrcidTokenApiResponse? payload = null;

                try
                {
                    payload =
                        await JsonSerializer
                            .DeserializeAsync<OrcidTokenApiResponse>(
                                stream,
                                JsonOptions,
                                cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "ORCID token endpoint returned invalid JSON.");

                    return Failure(
                        "INVALID_PROVIDER_RESPONSE",
                        "ORCID returned an invalid authentication response.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode =
                        (int)response.StatusCode;

                    _logger.LogWarning(
                        "ORCID token exchange failed with HTTP status {StatusCode}. Provider error: {ProviderError}",
                        statusCode,
                        payload?.Error);

                    if (response.StatusCode ==
                        HttpStatusCode.TooManyRequests)
                    {
                        return Failure(
                            "PROVIDER_RATE_LIMITED",
                            "ORCID is temporarily rate limited.");
                    }

                    if (statusCode >= 500 ||
                        response.StatusCode ==
                        HttpStatusCode.RequestTimeout)
                    {
                        return Failure(
                            "PROVIDER_UNAVAILABLE",
                            "ORCID is temporarily unavailable.");
                    }

                    return Failure(
                        "OAUTH_REJECTED",
                        "ORCID authentication was rejected.");
                }

                if (payload == null)
                {
                    return Failure(
                        "INVALID_PROVIDER_RESPONSE",
                        "ORCID returned an invalid authentication response.");
                }

                if (!OrcidIdUtility.TryNormalizeAndValidate(
                        payload.Orcid,
                        out var normalizedOrcidId))
                {
                    _logger.LogWarning(
                        "ORCID token endpoint returned an invalid authenticated ORCID iD.");

                    return Failure(
                        "INVALID_AUTHENTICATED_ORCID",
                        "ORCID returned an invalid authenticated ORCID iD.");
                }

                return new OrcidOAuthResult
                {
                    Success = true,
                    OrcidId = normalizedOrcidId,

                    DisplayName =
                        string.IsNullOrWhiteSpace(payload.Name)
                            ? null
                            : payload.Name.Trim()
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "ORCID token exchange timed out.");

                return Failure(
                    "PROVIDER_UNAVAILABLE",
                    "ORCID authentication timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "ORCID token exchange failed because of a network error.");

                return Failure(
                    "PROVIDER_UNAVAILABLE",
                    "ORCID is temporarily unavailable.");
            }
        }

        private void ValidateAuthorizationConfiguration()
        {
            if (string.IsNullOrWhiteSpace(
                    _settings.AuthorizationUrl))
            {
                throw new InvalidOperationException(
                    "ORCID AuthorizationUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                    _settings.ClientId))
            {
                throw new InvalidOperationException(
                    "ORCID ClientId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                    _settings.RedirectUri))
            {
                throw new InvalidOperationException(
                    "ORCID RedirectUri is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                    _settings.Scope))
            {
                throw new InvalidOperationException(
                    "ORCID Scope is not configured.");
            }
        }

        private void ValidateTokenConfiguration()
        {
            ValidateAuthorizationConfiguration();

            if (string.IsNullOrWhiteSpace(
                    _settings.TokenUrl))
            {
                throw new InvalidOperationException(
                    "ORCID TokenUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                    _settings.ClientSecret))
            {
                throw new InvalidOperationException(
                    "ORCID ClientSecret is not configured.");
            }
        }

        private static OrcidOAuthResult Failure(
            string errorCode,
            string errorMessage)
        {
            return new OrcidOAuthResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        private sealed class OrcidTokenApiResponse
        {
            [JsonPropertyName("orcid")]
            public string? Orcid { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }
        }
    }
}