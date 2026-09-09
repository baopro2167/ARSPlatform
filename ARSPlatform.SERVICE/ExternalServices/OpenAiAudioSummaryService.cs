using ARSPlatform.MODEL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class OpenAiAudioSummaryService : IOpenAiAudioSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiAudioSummaryService> _logger;

        private const long MaxSingleTranscriptionBytes = 24 * 1024 * 1024;
        private const int SegmentSeconds = 20 * 60;

        public OpenAiAudioSummaryService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<OpenAiAudioSummaryService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(15);
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SummarizeAsync(
            string compressedAudioPath,
            Seminar seminar,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(compressedAudioPath) || !File.Exists(compressedAudioPath))
                throw new FileNotFoundException("Không tìm thấy file audio đã nén để OpenAI xử lý.", compressedAudioPath);

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? _configuration["OpenAISettings:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Thiếu OPENAI_API_KEY trong Environment hoặc OpenAISettings:ApiKey.");

            var baseUrl = (_configuration["OpenAISettings:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
            var transcriptionModel = _configuration["OpenAISettings:TranscriptionModel"] ?? "gpt-transcribe";
            var summaryModel = _configuration["OpenAISettings:SummaryModel"] ?? "gpt-5.6-luna";
            var segmentPaths = await PrepareSegmentsAsync(compressedAudioPath, cancellationToken);

            try
            {
                _logger.LogInformation(
                    "OpenAI fallback prepared {SegmentCount} audio segment(s). SeminarId={SeminarId}",
                    segmentPaths.Count,
                    seminar.SeminarId);

                var transcriptBuilder = new StringBuilder();

                for (var i = 0; i < segmentPaths.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogInformation(
                        "OpenAI transcription started. SeminarId={SeminarId}, Segment={Segment}/{SegmentCount}",
                        seminar.SeminarId,
                        i + 1,
                        segmentPaths.Count);

                    var segmentTranscript = await TranscribeSegmentAsync(
                        segmentPaths[i],
                        apiKey,
                        baseUrl,
                        transcriptionModel,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(segmentTranscript))
                    {
                        if (transcriptBuilder.Length > 0)
                            transcriptBuilder.AppendLine().AppendLine();

                        transcriptBuilder.Append(segmentTranscript.Trim());
                    }
                }

                var transcript = transcriptBuilder.ToString().Trim();

                if (string.IsNullOrWhiteSpace(transcript))
                    throw new HttpRequestException("OpenAI transcription không trả về nội dung transcript.");

                _logger.LogInformation(
                    "OpenAI transcription completed. SeminarId={SeminarId}, TranscriptLength={TranscriptLength}",
                    seminar.SeminarId,
                    transcript.Length);

                var summary = await GenerateSummaryAsync(
                    transcript,
                    seminar,
                    apiKey,
                    baseUrl,
                    summaryModel,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(summary))
                    throw new HttpRequestException("OpenAI Responses API không trả về nội dung tóm tắt.");

                return summary.Trim();
            }
            finally
            {
                foreach (var segmentPath in segmentPaths)
                {
                    if (!string.Equals(segmentPath, compressedAudioPath, StringComparison.OrdinalIgnoreCase))
                        DeleteLocalFile(segmentPath);
                }
            }
        }

        private async Task<List<string>> PrepareSegmentsAsync(
            string compressedAudioPath,
            CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(compressedAudioPath);

            if (fileInfo.Length <= MaxSingleTranscriptionBytes)
                return new List<string> { compressedAudioPath };

            var segmentPrefix = $"{Guid.NewGuid():N}-openai-segment";
            var outputPattern = Path.Combine(Path.GetTempPath(), $"{segmentPrefix}-%03d.mp3");

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{compressedAudioPath}\" -f segment -segment_time {SegmentSeconds} -reset_timestamps 1 -c copy -y \"{outputPattern}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không thể chạy FFmpeg để chia audio cho OpenAI transcription.",
                    ex);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            _ = await outputTask;
            var error = await errorTask;

            var segmentPaths = Directory
                .GetFiles(Path.GetTempPath(), $"{segmentPrefix}-*.mp3")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (process.ExitCode != 0 || segmentPaths.Count == 0)
            {
                foreach (var segmentPath in segmentPaths)
                    DeleteLocalFile(segmentPath);

                throw new InvalidOperationException(
                    $"Lỗi chia audio cho OpenAI bằng FFmpeg (Code {process.ExitCode}): {error}");
            }

            return segmentPaths;
        }

        private async Task<string> TranscribeSegmentAsync(
            string segmentPath,
            string apiKey,
            string baseUrl,
            string model,
            CancellationToken cancellationToken)
        {
            var url = $"{baseUrl}/audio/transcriptions";

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var fileStream = File.OpenRead(segmentPath);
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                form.Add(fileContent, "file", Path.GetFileName(segmentPath));
                form.Add(new StringContent(model), "model");
                form.Add(new StringContent("json"), "response_format");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = form;

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var document = JsonDocument.Parse(responseJson);

                    if (document.RootElement.TryGetProperty("text", out var textElement) &&
                        textElement.ValueKind == JsonValueKind.String)
                    {
                        return textElement.GetString()?.Trim() ?? string.Empty;
                    }

                    throw new HttpRequestException("OpenAI transcription response không chứa trường text.");
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (IsRetryableStatusCode((int)response.StatusCode) && attempt < 3)
                {
                    var delaySeconds = attempt * 2;

                    _logger.LogWarning(
                        "OpenAI transcription tạm thời lỗi {StatusCode}. Thử lại lần {Attempt}/3 sau {DelaySeconds}s.",
                        (int)response.StatusCode,
                        attempt,
                        delaySeconds);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                throw new HttpRequestException(
                    $"OpenAI transcription thất bại. StatusCode={(int)response.StatusCode}. Response={errorBody}",
                    null,
                    response.StatusCode);
            }

            throw new HttpRequestException("Không thể transcription audio bằng OpenAI sau 3 lần thử.");
        }

        private async Task<string> GenerateSummaryAsync(
            string transcript,
            Seminar seminar,
            string apiKey,
            string baseUrl,
            string model,
            CancellationToken cancellationToken)
        {
            var prompt = BuildSummaryPrompt(seminar, transcript);
            var requestBody = new
            {
                model,
                instructions = "You create accurate, structured academic seminar summaries. Use only the supplied transcript and metadata. Do not invent names, findings, numbers, decisions, deadlines, or action owners. Return clean Markdown only.",
                input = prompt
            };

            var url = $"{baseUrl}/responses";

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var document = JsonDocument.Parse(responseJson);
                    var summary = ExtractResponseText(document.RootElement);

                    if (!string.IsNullOrWhiteSpace(summary))
                        return summary.Trim();

                    throw new HttpRequestException("OpenAI Responses API response không chứa output_text.");
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (IsRetryableStatusCode((int)response.StatusCode) && attempt < 3)
                {
                    var delaySeconds = attempt * 2;

                    _logger.LogWarning(
                        "OpenAI summary tạm thời lỗi {StatusCode}. Thử lại lần {Attempt}/3 sau {DelaySeconds}s.",
                        (int)response.StatusCode,
                        attempt,
                        delaySeconds);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                throw new HttpRequestException(
                    $"OpenAI Responses API thất bại. StatusCode={(int)response.StatusCode}. Response={errorBody}",
                    null,
                    response.StatusCode);
            }

            throw new HttpRequestException("Không thể tạo bản tóm tắt bằng OpenAI sau 3 lần thử.");
        }

        private static string BuildSummaryPrompt(Seminar seminar, string transcript)
        {
            return $"""
Seminar metadata:
- Seminar ID: {seminar.SeminarId}
- Seminar content/title: {seminar.Content}
- Start time: {seminar.StartTime:yyyy-MM-dd HH:mm:ss}
- End time: {seminar.EndTime:yyyy-MM-dd HH:mm:ss}

Transcript:
```text
{transcript}
```

Create a professional seminar summary in the same language predominantly used in the transcript.

Required Markdown structure:
# Seminar Summary
## Overview
## Key Takeaways
## Main Discussion
## Questions, Critiques, and Responses
## Decisions and Agreements
## Important Data, Evidence, and References
## Action Items
## Open Questions and Follow-up Topics
## Final Summary

Rules:
- Read the entire transcript before summarizing.
- Use only information in the transcript or metadata above.
- Do not invent or guess names, roles, affiliations, dates, numerical results, citations, decisions, deadlines, or action owners.
- If a person cannot be identified reliably, describe the speaker generically instead of inventing an identity.
- Clearly separate presented information, questions, responses, confirmed decisions, unresolved issues, and explicit action items.
- Preserve important technical terms, methods, model names, datasets, tools, metrics, and numerical findings as stated.
- If a required section has no supporting information, explicitly say that no such information was recorded.
- Return Markdown only.
""";
        }

        private static string? ExtractResponseText(JsonElement rootElement)
        {
            if (rootElement.TryGetProperty("output_text", out var outputTextElement) &&
                outputTextElement.ValueKind == JsonValueKind.String)
            {
                var outputText = outputTextElement.GetString();

                if (!string.IsNullOrWhiteSpace(outputText))
                    return outputText;
            }

            if (!rootElement.TryGetProperty("output", out var outputElement) ||
                outputElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var textBuilder = new StringBuilder();

            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement) ||
                    contentElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (!contentItem.TryGetProperty("type", out var typeElement) ||
                        !string.Equals(typeElement.GetString(), "output_text", StringComparison.OrdinalIgnoreCase) ||
                        !contentItem.TryGetProperty("text", out var textElement) ||
                        textElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var text = textElement.GetString();

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    if (textBuilder.Length > 0)
                        textBuilder.AppendLine();

                    textBuilder.Append(text);
                }
            }

            return textBuilder.Length == 0 ? null : textBuilder.ToString();
        }

        private static bool IsRetryableStatusCode(int statusCode)
        {
            return statusCode == 408
                || statusCode == 409
                || statusCode == 429
                || statusCode >= 500;
        }

        private static void DeleteLocalFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch
            {
                // Cleanup failure must not break the primary request.
            }
        }
    }
}
