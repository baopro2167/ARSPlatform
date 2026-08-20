using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class GoogleMeetService : IGoogleMeetService
    {
        private const string GoogleOAuthTokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string GoogleMeetCreateSpaceEndpoint = "https://meet.googleapis.com/v2/spaces";

        private readonly HttpClient _httpClient;
        private readonly GoogleMeetSettings _settings;

        public GoogleMeetService(
            HttpClient httpClient,
            IOptions<GoogleMeetSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<string> CreateMeetingSpaceAsync(CancellationToken cancellationToken = default)
        {
            ValidateSettings();

            var accessToken = await GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                GoogleMeetCreateSpaceEndpoint);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent(
                "{}",
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Google Meet space creation failed with status {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("meetingUri", out var meetingUriElement))
            {
                throw new HttpRequestException(
                    "Google Meet API did not return a meeting URI.");
            }

            var meetingUri = meetingUriElement.GetString();

            if (string.IsNullOrWhiteSpace(meetingUri)
                || !meetingUri.StartsWith(
                    "https://meet.google.com/",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException(
                    "Google Meet API returned an invalid meeting URI.");
            }

            return meetingUri;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                GoogleOAuthTokenEndpoint)
            {
                Content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["client_id"] = _settings.ClientId,
                        ["client_secret"] = _settings.ClientSecret,
                        ["refresh_token"] = _settings.RefreshToken,
                        ["grant_type"] = "refresh_token"
                    })
            };

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Google OAuth token request failed with status {(int)response.StatusCode}. Response: {responseBody}");
            }

            using var document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                throw new HttpRequestException(
                    "Google OAuth token response did not contain an access token.");
            }

            var accessToken = accessTokenElement.GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new HttpRequestException(
                    "Google OAuth token response contained an empty access token.");
            }

            return accessToken;
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.ClientId)
                || string.IsNullOrWhiteSpace(_settings.ClientSecret)
                || string.IsNullOrWhiteSpace(_settings.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Google Meet integration is not configured.");
            }
        }
    }
}