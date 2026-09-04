using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    public class AudioSummaryService : IAudioSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AudioSummaryService> _logger;
        private readonly ISeminarRepository _seminarRepository;

        private const long MaxUploadSizeBytes = 524_288_000;
        private const double MaxMediaDurationSeconds = 7200;

        private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedContentTypesByExtension =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [".mp3"] = new(StringComparer.OrdinalIgnoreCase) { "audio/mpeg", "audio/mp3", "audio/x-mp3", "application/octet-stream" },
                [".wav"] = new(StringComparer.OrdinalIgnoreCase) { "audio/wav", "audio/x-wav", "audio/wave", "audio/vnd.wave", "application/octet-stream" },
                [".m4a"] = new(StringComparer.OrdinalIgnoreCase) { "audio/mp4", "audio/x-m4a", "application/octet-stream" },
                [".aac"] = new(StringComparer.OrdinalIgnoreCase) { "audio/aac", "audio/x-aac", "application/octet-stream" },
                [".ogg"] = new(StringComparer.OrdinalIgnoreCase) { "audio/ogg", "application/ogg", "application/octet-stream" },
                [".flac"] = new(StringComparer.OrdinalIgnoreCase) { "audio/flac", "audio/x-flac", "application/octet-stream" },
                [".mp4"] = new(StringComparer.OrdinalIgnoreCase) { "video/mp4", "audio/mp4", "application/mp4", "application/octet-stream" }
            };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AudioSummaryService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AudioSummaryService> logger,
            ISeminarRepository seminarRepository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _seminarRepository = seminarRepository;
        }

        public async Task<SeminarAudioSummaryResponse> SummarizeSeminarAudioAsync(
            int seminarId,
            SeminarAudioSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            var file = request.AudioFile;

            if (file == null || file.Length == 0)
                throw new ArgumentException("File âm thanh không hợp lệ.", nameof(file));

            var inputExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedContentTypesByExtension.TryGetValue(inputExtension, out var allowedContentTypes))
                throw new ArgumentException("Chỉ chấp nhận file MP3, WAV, M4A, AAC, OGG, FLAC hoặc MP4.", nameof(file));

            var contentType = file.ContentType?.Trim();

            if (string.IsNullOrWhiteSpace(contentType) || !allowedContentTypes.Contains(contentType))
                throw new ArgumentException($"Content-Type '{file.ContentType}' không hợp lệ cho file {inputExtension}.", nameof(file));

            if (file.Length > MaxUploadSizeBytes)
                throw new ArgumentException("File không được vượt quá 500 MB.", nameof(file));

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? _configuration["GeminiSettings:ApiKey"];

            var model = _configuration["GeminiSettings:Model"] ?? "gemini-3.7-flash";

            if (string.IsNullOrWhiteSpace(apiKey) ||
                string.Equals(apiKey, "REPLACE_WITH_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Thiếu GEMINI_API_KEY hợp lệ trong Environment hoặc GeminiSettings:ApiKey.");
            }

            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("Thiếu GeminiSettings:Model.");

            var seminar = await _seminarRepository.GetByIdAsync(seminarId);

            if (seminar == null)
                throw new KeyNotFoundException($"Không tìm thấy Seminar với ID = {seminarId}");

            string tempInputPath = string.Empty;
            string tempCompressedPath = string.Empty;
            string? resourceName = null;

            var diagnosticTimer = Stopwatch.StartNew();
            var currentStage = "START";

            _logger.LogInformation(
                "AI Summary started. SeminarId={SeminarId}, FileName={FileName}, Extension={Extension}, ContentType={ContentType}, SizeBytes={SizeBytes}",
                seminarId,
                file.FileName,
                inputExtension,
                contentType,
                file.Length);

            try
            {
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{inputExtension}");

                await using (var stream = new FileStream(tempInputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                currentStage = "MEDIA_SAVED";

                _logger.LogInformation(
                    "AI Summary input saved. SeminarId={SeminarId}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    diagnosticTimer.ElapsedMilliseconds);

                currentStage = "FFPROBE";

                var durationSeconds = await GetMediaDurationSecondsAsync(tempInputPath, cancellationToken);

                _logger.LogInformation(
                    "AI Summary ffprobe completed. SeminarId={SeminarId}, DurationSeconds={DurationSeconds}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    durationSeconds,
                    diagnosticTimer.ElapsedMilliseconds);

                if (durationSeconds >= MaxMediaDurationSeconds)
                    throw new ArgumentException("File ghi âm phải có thời lượng dưới 2 giờ.", nameof(file));

                currentStage = "FFMPEG";

                tempCompressedPath = await CompressAudioAsync(tempInputPath, cancellationToken);

                _logger.LogInformation(
                    "AI Summary FFmpeg completed. SeminarId={SeminarId}, CompressedSizeBytes={CompressedSizeBytes}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    new FileInfo(tempCompressedPath).Length,
                    diagnosticTimer.ElapsedMilliseconds);

                currentStage = "GOOGLE_UPLOAD";

                var (fileUri, uploadedResourceName) =
                    await UploadToGoogleFilesApiAsync(tempCompressedPath, apiKey, cancellationToken);

                resourceName = uploadedResourceName;

                _logger.LogInformation(
                    "AI Summary Google upload completed. SeminarId={SeminarId}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    diagnosticTimer.ElapsedMilliseconds);

                currentStage = "GOOGLE_FILE_PROCESSING";

                await WaitForFileActiveAsync(resourceName, apiKey, cancellationToken);

                _logger.LogInformation(
                    "AI Summary Google file ACTIVE. SeminarId={SeminarId}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    diagnosticTimer.ElapsedMilliseconds);

                currentStage = "GEMINI";

                var summaryMarkdown = await GenerateSummaryTextAsync(
                    fileUri,
                    apiKey,
                    model,
                    cancellationToken);

                _logger.LogInformation(
                    "AI Summary Gemini completed. SeminarId={SeminarId}, SummaryLength={SummaryLength}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    summaryMarkdown.Length,
                    diagnosticTimer.ElapsedMilliseconds);

                seminar.AiSummary = summaryMarkdown;
                _seminarRepository.Update(seminar);

                currentStage = "DATABASE_SAVE";

                await _seminarRepository.SaveChangesAsync();

                currentStage = "COMPLETED";

                _logger.LogInformation(
                    "AI Summary completed successfully. SeminarId={SeminarId}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    diagnosticTimer.ElapsedMilliseconds);

                return new SeminarAudioSummaryResponse
                {
                    SeminarId = seminar.SeminarId,
                    AiSummary = seminar.AiSummary ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI Summary failed. SeminarId={SeminarId}, Stage={Stage}, ElapsedMs={ElapsedMs}",
                    seminarId,
                    currentStage,
                    diagnosticTimer.ElapsedMilliseconds);

                throw;
            }
            finally
            {
                DeleteLocalFile(tempInputPath);
                DeleteLocalFile(tempCompressedPath);

                if (!string.IsNullOrWhiteSpace(resourceName))
                    _ = DeleteGoogleFileAsync(resourceName, apiKey);
            }
        }

        private async Task<double> GetMediaDurationSecondsAsync(
            string inputPath,
            CancellationToken cancellationToken)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputPath}\"",
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
                    "Không thể chạy ffprobe. Hãy kiểm tra FFmpeg/ffprobe đã được cài đặt và có trong PATH.",
                    ex);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();

            if (process.ExitCode != 0)
            {
                throw new ArgumentException(
                    string.IsNullOrWhiteSpace(error)
                        ? "Không thể đọc thời lượng file media."
                        : $"Không thể đọc thời lượng file media: {error}");
            }

            if (!double.TryParse(
                    output,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var durationSeconds))
            {
                throw new ArgumentException("Không thể xác định thời lượng file media.");
            }

            if (durationSeconds <= 0)
                throw new ArgumentException("File media có thời lượng không hợp lệ.");

            return durationSeconds;
        }

        private async Task<string> CompressAudioAsync(
            string inputPath,
            CancellationToken cancellationToken)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputPath}\" -vn -acodec libmp3lame -ab 32k -ar 16000 -ac 1 -y \"{outputPath}\"",
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
                    "Không thể chạy FFmpeg. Hãy kiểm tra FFmpeg đã được cài đặt và có trong PATH.",
                    ex);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            _ = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                DeleteLocalFile(outputPath);
                throw new InvalidOperationException($"Lỗi xử lý audio bằng FFmpeg (Code {process.ExitCode}): {error}");
            }

            return outputPath;
        }

        private async Task<(string FileUri, string ResourceName)> UploadToGoogleFilesApiAsync(
            string filePath,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            const string initUrl = "https://generativelanguage.googleapis.com/upload/v1beta/files";

            using var initRequest = new HttpRequestMessage(HttpMethod.Post, initUrl);

            AddApiKeyHeader(initRequest, apiKey);

            initRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            initRequest.Headers.Add("X-Goog-Upload-Command", "start");
            initRequest.Headers.Add(
                "X-Goog-Upload-Header-Content-Length",
                fileInfo.Length.ToString(CultureInfo.InvariantCulture));
            initRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", "audio/mpeg");

            var metadata = new
            {
                file = new
                {
                    display_name = Path.GetFileName(filePath)
                }
            };

            initRequest.Content = new StringContent(
                JsonSerializer.Serialize(metadata, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var initResponse = await _httpClient.SendAsync(initRequest, cancellationToken);

            if (!initResponse.IsSuccessStatusCode)
            {
                var errorBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);

                throw new HttpRequestException(
                    $"Google Files API khởi tạo upload thất bại. StatusCode={(int)initResponse.StatusCode}. Response={errorBody}");
            }

            if (!initResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrls))
                throw new HttpRequestException("Google Files API không trả về X-Goog-Upload-URL.");

            var uploadUrl = uploadUrls.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(uploadUrl))
                throw new HttpRequestException("Google Files API trả về upload URL không hợp lệ.");

            await using var fileStream = File.OpenRead(filePath);
            using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

            uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");

            uploadRequest.Content = new StreamContent(fileStream);
            uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            uploadRequest.Content.Headers.ContentLength = fileInfo.Length;

            using var uploadResponse = await _httpClient.SendAsync(uploadRequest, cancellationToken);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

                throw new HttpRequestException(
                    $"Google Files API upload file thất bại. StatusCode={(int)uploadResponse.StatusCode}. Response={errorBody}");
            }

            var responseJson = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);

            if (!document.RootElement.TryGetProperty("file", out var fileElement))
                throw new HttpRequestException("Google Files API response không chứa thông tin file.");

            if (!fileElement.TryGetProperty("uri", out var uriElement) ||
                !fileElement.TryGetProperty("name", out var nameElement))
            {
                throw new HttpRequestException("Google Files API response thiếu uri hoặc name.");
            }

            var fileUri = uriElement.GetString();
            var resourceName = nameElement.GetString();

            if (string.IsNullOrWhiteSpace(fileUri) || string.IsNullOrWhiteSpace(resourceName))
                throw new HttpRequestException("Google Files API trả về thông tin file không hợp lệ.");

            return (fileUri, resourceName);
        }

        private async Task WaitForFileActiveAsync(
            string resourceName,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/{resourceName}";

            for (var attempt = 0; attempt < 60; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddApiKeyHeader(request, apiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var document = JsonDocument.Parse(responseJson);

                    if (document.RootElement.TryGetProperty("state", out var stateElement))
                    {
                        var state = stateElement.GetString();

                        if (string.Equals(state, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                            return;

                        if (string.Equals(state, "FAILED", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Google Files API không thể xử lý file âm thanh.");
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogWarning(
                        "Google Files API status check failed. ResourceName={ResourceName}, StatusCode={StatusCode}, Response={Response}",
                        resourceName,
                        (int)response.StatusCode,
                        errorBody);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            throw new TimeoutException("Hệ thống Google xử lý file âm thanh quá lâu.");
        }

        private async Task<string> GenerateSummaryTextAsync(
            string fileUri,
            string apiKey,
            string model,
            CancellationToken cancellationToken)
        {
            var cleanModel = model
                .Replace("models/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanModel))
                throw new InvalidOperationException("GeminiSettings:Model không hợp lệ.");

            const string academicSummaryPrompt = """
## Seminar Recording Summary System Prompt

You are an AI assistant that creates accurate, structured, and shareable summaries of academic seminars, research meetings, and conferences.

Your audience includes participants and people who did not attend the session. Produce a concise but sufficiently detailed record of the seminar’s important content, discussion, decisions, questions, and follow-up work.

### Input

Seminar metadata:

- Title: `{{SEMINAR_TITLE}}`
- Date/time: `{{SEMINAR_DATE}}`
- Host: `{{HOST_NAME}}`
- Known participants or speaker information: `{{PARTICIPANT_METADATA}}`
- Required output language: `{{OUTPUT_LANGUAGE}}`

Transcript:

```text
{{TRANSCRIPT}}
```

### Core rules

1. Read and analyze the entire transcript before writing the summary.
2. Use only information present in the transcript or the supplied metadata.
3. Do not invent, infer, or guess names, roles, affiliations, dates, decisions, research results, numerical values, citations, deadlines, or action-item owners.
4. If a speaker cannot be identified reliably, use `Speaker 1`, `Speaker 2`, and so on. Use a role only when it is clearly established.
5. Include timestamps only when they are provided reliably in the input. Never create approximate timestamps.
6. Preserve important technical terms, research methods, model names, datasets, metrics, tools, numerical findings, and citations exactly as stated. If translating, retain the original technical term when useful.
7. Clearly distinguish between:
   - presented information or findings;
   - questions and critiques;
   - responses;
   - confirmed decisions;
   - unresolved issues;
   - explicit action items.
8. Do not describe speculation as a conclusion. If the transcript is uncertain or incomplete, state that it is unclear or not specified.
9. Do not include an introduction about being an AI. Do not repeat the same information across sections.
10. Write in natural, professional `{{OUTPUT_LANGUAGE}}` and return clean Markdown only. Avoid unnecessary decorative symbols.

### Required output format

# {{SEMINAR_TITLE | default: "Seminar Summary"}}

## Overview

Provide a short summary of:

- The seminar topic and objective
- The presenter(s) or roles, only if reliably identified
- The most important themes and conclusions

## Key Takeaways

List the most important information a participant should remember. Focus on findings, arguments, decisions, methods, risks, and implications rather than repeating the transcript.

## Main Discussion

Present the seminar content in the order it occurred.

For each major topic, include:

- Topic or discussion point
- Speaker or role, if known
- Main argument, method, result, or explanation
- Relevant response, clarification, or critique

Use clear subsections when the seminar covers multiple topics.

## Questions, Critiques, and Responses

Separate notable questions, concerns, critiques, and answers.

For each item, include:

- Question, concern, or critique
- Person or role raising it, if known
- Response and responder, if known
- Whether the issue was resolved, partially resolved, or remains open

If none were recorded, write: `No notable questions or critiques were recorded.`

## Decisions and Agreements

List only decisions that were explicitly confirmed during the seminar.

For each decision, state:

| Decision | Context or rationale | Confirmed by |
|---|---|---|

If no explicit decisions were made, write: `No explicit decisions or agreements were recorded.`

## Important Data, Evidence, and References

List only information explicitly mentioned in the transcript, such as:

- Numerical results, metrics, or experimental outcomes
- Research methods, models, datasets, or tools
- Papers, sources, systems, or materials referenced
- Limitations or evidence gaps

If none were mentioned, write: `No specific data, evidence, or references were recorded.`

## Action Items

List only explicit follow-up work, assignments, or deadlines.

| Action | Owner | Deadline | Notes |
|---|---|---|---|

If there are no explicit action items, write: `No specific action items were recorded.`

## Open Questions and Follow-up Topics

List unresolved questions, risks, disagreements, or topics that require future discussion.

If none were identified, write: `No unresolved follow-up topics were recorded.`

## Final Summary

Write a brief closing paragraph that captures the seminar’s overall conclusion and the most important next consideration for participants.
""";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                fileData = new
                                {
                                    mimeType = "audio/mpeg",
                                    fileUri
                                }
                            },
                            new
                            {
                                text = academicSummaryPrompt
                            }
                        }
                    }
                }
            };

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent";

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                AddApiKeyHeader(request, apiKey);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    using var document = JsonDocument.Parse(responseJson);
                    var summaryText = ExtractGeminiText(document.RootElement);

                    if (string.IsNullOrWhiteSpace(summaryText))
                        throw new HttpRequestException("Gemini API không trả về nội dung tóm tắt.");

                    return summaryText.Trim();
                }

                var statusCode = (int)response.StatusCode;
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (statusCode == 429 || statusCode == 503)
                {
                    if (attempt < 3)
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
                        "Gemini model '{Model}' vẫn trả lỗi {StatusCode} sau 3 lần thử. Response={Response}",
                        cleanModel,
                        statusCode,
                        errorBody);

                    throw new HttpRequestException(
                        $"Gemini model '{cleanModel}' tạm thời không khả dụng sau 3 lần thử. StatusCode={statusCode}.");
                }

                if (statusCode == 404)
                {
                    _logger.LogWarning(
                        "Gemini model '{Model}' trả 404. Response={Response}",
                        cleanModel,
                        errorBody);

                    throw new HttpRequestException(
                        $"Gemini model cấu hình '{cleanModel}' không tồn tại hoặc không hỗ trợ generateContent.");
                }

                _logger.LogWarning(
                    "Gemini model '{Model}' trả lỗi {StatusCode}. Response={Response}",
                    cleanModel,
                    statusCode,
                    errorBody);

                throw new HttpRequestException(
                    $"Gemini API trả lỗi {statusCode} khi sử dụng model '{cleanModel}'.");
            }

            throw new HttpRequestException(
                $"Không thể tạo bản tóm tắt bằng Gemini model '{cleanModel}'.");
        }

        private static string? ExtractGeminiText(JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return null;
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (!part.TryGetProperty("text", out var textElement) ||
                        textElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var text = textElement.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            return null;
        }

        private async Task DeleteGoogleFileAsync(string resourceName, string apiKey)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/{resourceName}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                AddApiKeyHeader(request, apiKey);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Không thể xóa temporary Google file {ResourceName}. StatusCode={StatusCode}",
                        resourceName,
                        (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể cleanup temporary Google file {ResourceName}.",
                    resourceName);
            }
        }

        private static void AddApiKeyHeader(HttpRequestMessage request, string apiKey)
        {
            request.Headers.Add("x-goog-api-key", apiKey);
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