using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public interface IGoogleCalendarService
    {
        Task<string> CreateGoogleMeetLinkAsync(string summary, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    }

    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleCalendarSettings _settings;

        private static string _cachedAccessToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        public GoogleCalendarService(HttpClient httpClient, GoogleCalendarSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
                {
                    return _cachedAccessToken;
                }

                var jwtToken = CreateJwtToken();
                _cachedAccessToken = await ExchangeJwtForAccessTokenAsync(jwtToken, cancellationToken);
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55);

                return _cachedAccessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private string CreateJwtToken()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiry = now + 3600;

            var header = Base64UrlEncode("{\"alg\":\"RS256\",\"typ\":\"JWT\"}");
            var payload = Base64UrlEncode($"{{\"iss\":\"{_settings.ServiceAccountEmail}\",\"scope\":\"https://www.googleapis.com/auth/calendar.events https://www.googleapis.com/auth/calendar.readonly\",\"aud\":\"https://oauth2.googleapis.com/token\",\"iat\":{now},\"exp\":{expiry}}}");

            var signatureInput = $"{header}.{payload}";
            var signature = SignWithRsa(signatureInput, _settings.PrivateKey);

            return $"{signatureInput}.{signature}";
        }

        private string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private string SignWithRsa(string data, string privateKeyPem)
        {
            using var rsa = System.Security.Cryptography.RSA.Create();
            
            privateKeyPem = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "");

            var keyBytes = Convert.FromBase64String(privateKeyPem);
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);

            var signature = rsa.SignData(Encoding.UTF8.GetBytes(data), System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return Base64UrlEncode(Convert.ToBase64String(signature));
        }

        private async Task<string> ExchangeJwtForAccessTokenAsync(string jwt, CancellationToken cancellationToken)
        {
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth2:grant-type:jwt-bearer" },
                { "assertion", jwt }
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get access token: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return result.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("No access_token in response");
        }

        public async Task<string> CreateGoogleMeetLinkAsync(string summary, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var eventRequest = new
            {
                summary = summary,
                description = "Created by ARSPlatform",
                start = new
                {
                    dateTime = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timeZone = "UTC"
                },
                end = new
                {
                    dateTime = endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timeZone = "UTC"
                },
                conferenceData = new
                {
                    createRequest = new
                    {
                        requestId = Guid.NewGuid().ToString(),
                        conferenceSolutionKey = new
                        {
                            type = "hangoutsMeet"
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(eventRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/calendar/v3/calendars/primary/events?conferenceDataVersion=1")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to create Meet event: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (result.TryGetProperty("conferenceData", out var conferenceData) &&
                conferenceData.TryGetProperty("entryPoints", out var entryPoints))
            {
                foreach (var entry in entryPoints.EnumerateArray())
                {
                    if (entry.TryGetProperty("entryPointType", out var type) && type.GetString() == "video")
                    {
                        return entry.GetProperty("uri").GetString() ?? "";
                    }
                }
            }

            if (result.TryGetProperty("hangoutLink", out var hangoutLink))
            {
                return hangoutLink.GetString() ?? "";
            }

            throw new InvalidOperationException("No meeting link found in response");
        }
    }
}
