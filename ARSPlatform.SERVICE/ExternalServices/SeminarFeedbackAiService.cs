using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class SeminarFeedbackAiService : ISeminarFeedbackAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeminarFeedbackAiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SeminarFeedbackAiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SeminarFeedbackAiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SeminarFeedbackAiSummaryContentResponse> SummarizeFeedbackAsync(
            string seminarContent,
            IReadOnlyCollection<string> feedbackJsons,
            CancellationToken cancellationToken = default)
        {
            if (feedbackJsons == null || feedbackJsons.Count == 0)
                throw new ArgumentException("Không có feedback để tổng hợp.", nameof(feedbackJsons));

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? _configuration["GeminiSettings:ApiKey"];

            var model = _configuration["GeminiSettings:Model"] ?? "gemini-3.7-flash";
            var baseUrl = _configuration["GeminiSettings:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta";

            if (string.IsNullOrWhiteSpace(apiKey)
                || string.Equals(apiKey, "REPLACE_WITH_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Thiếu GEMINI_API_KEY hợp lệ trong Environment hoặc GeminiSettings:ApiKey.");
            }

            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("Thiếu GeminiSettings:Model.");

            var cleanModel = model.Replace("models/", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

            if (string.IsNullOrWhiteSpace(cleanModel))
                throw new InvalidOperationException("GeminiSettings:Model không hợp lệ.");

            var normalizedFeedbacks = new List<JsonElement>();

            foreach (var feedbackJson in feedbackJsons.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    using var document = JsonDocument.Parse(feedbackJson);
                    normalizedFeedbacks.Add(document.RootElement.Clone());
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Bỏ qua một FeedbackJson không hợp lệ khi tổng hợp Seminar feedback.");
                }
            }

            if (normalizedFeedbacks.Count == 0)
                throw new ArgumentException("Không có FeedbackJson hợp lệ để tổng hợp.", nameof(feedbackJsons));

            var feedbackPayload = JsonSerializer.Serialize(normalizedFeedbacks, JsonOptions);
            var prompt = BuildPrompt(seminarContent, feedbackPayload);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"{baseUrl.TrimEnd('/')}/models/{cleanModel}:generateContent";

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-goog-api-key", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(responseBody);
                    var text = ExtractGeminiText(document.RootElement);

                    if (string.IsNullOrWhiteSpace(text))
                        throw new HttpRequestException("Gemini API không trả về nội dung tổng hợp feedback.");

                    var cleanJson = StripJsonCodeFence(text);

                    try
                    {
                        var result = JsonSerializer.Deserialize<SeminarFeedbackAiSummaryContentResponse>(
                            cleanJson,
                            JsonOptions);

                        if (result == null)
                            throw new JsonException("Gemini trả về JSON rỗng.");

                        result.CommonStrengths ??= new List<string>();
                        result.AreasForImprovement ??= new List<string>();
                        result.CommonSuggestions ??= new List<string>();
                        result.ConflictingFeedback ??= new List<string>();
                        result.RecommendedActions ??= new List<string>();
                        result.OverallAssessment ??= string.Empty;

                        return result;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Gemini trả về feedback summary không đúng JSON schema.");

                        throw new HttpRequestException(
                            "Gemini trả về dữ liệu tổng hợp feedback không đúng định dạng JSON.",
                            ex);
                    }
                }

                var statusCode = (int)response.StatusCode;

                if ((statusCode == 429 || statusCode == 503) && attempt < 3)
                {
                    var delaySeconds = attempt * 4;

                    _logger.LogWarning(
                        "Gemini model '{Model}' tạm thời không khả dụng ({StatusCode}). Thử lại lần {Attempt}/3 sau {DelaySeconds}s.",
                        cleanModel,
                        statusCode,
                        attempt,
                        delaySeconds);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Gemini feedback summary failed. Model={Model}, StatusCode={StatusCode}",
                    cleanModel,
                    statusCode);

                if (statusCode == 404)
                {
                    throw new HttpRequestException(
                        $"Gemini model cấu hình '{cleanModel}' không tồn tại hoặc không hỗ trợ generateContent.");
                }

                throw new HttpRequestException(
                    $"Gemini API trả lỗi {statusCode} khi tổng hợp Seminar feedback bằng model '{cleanModel}'.");
            }

            throw new HttpRequestException(
                $"Không thể tổng hợp Seminar feedback bằng Gemini model '{cleanModel}'.");
        }

        private static string BuildPrompt(string seminarContent, string feedbackPayload)
        {
            return $$"""
Bạn là trợ lý AI chuyên tổng hợp phản hồi sau seminar học thuật.

Chủ đề/nội dung Seminar:
{{seminarContent}}

Danh sách feedback ẩn danh của người tham dự ở dạng JSON:
{{feedbackPayload}}

Yêu cầu:
- Chỉ sử dụng thông tin có trong các feedback được cung cấp, không suy đoán danh tính hoặc thông tin ngoài dữ liệu.
- Không liệt kê lại từng participant và không copy nguyên văn hàng loạt feedback.
- Chỉ gọi một ý là phổ biến/common khi nhiều feedback thực sự cùng thể hiện ý đó.
- Nếu các feedback có quan điểm trái ngược, ghi riêng trong conflictingFeedback thay vì tạo kết luận đồng thuận giả.
- recommendedActions phải là hành động cải thiện có căn cứ trực tiếp từ feedback.
- Viết bằng tiếng Việt tự nhiên, ngắn gọn, chuyên nghiệp.
- Trả về đúng một JSON object hợp lệ, không Markdown, không code fence, không thêm lời giải thích ngoài JSON.

JSON schema bắt buộc:
{
  "overallAssessment": "string",
  "commonStrengths": ["string"],
  "areasForImprovement": ["string"],
  "commonSuggestions": ["string"],
  "conflictingFeedback": ["string"],
  "recommendedActions": ["string"]
}
""";
        }

        private static string? ExtractGeminiText(JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty("candidates", out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content)
                    || !content.TryGetProperty("parts", out var parts)
                    || parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textElement)
                        && textElement.ValueKind == JsonValueKind.String)
                    {
                        var text = textElement.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                            return text;
                    }
                }
            }

            return null;
        }

        private static string StripJsonCodeFence(string text)
        {
            var value = text.Trim();

            if (!value.StartsWith("```", StringComparison.Ordinal))
                return value;

            var firstLineEnd = value.IndexOf('\n');

            if (firstLineEnd >= 0)
                value = value[(firstLineEnd + 1)..];

            var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);

            if (lastFence >= 0)
                value = value[..lastFence];

            return value.Trim();
        }
    }
}