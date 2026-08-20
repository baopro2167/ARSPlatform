using ARSPlatform.MODEL.Entities;
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
        private const double MaxVideoDurationSeconds = 7200;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
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
            {
                throw new ArgumentException(
                    "File âm thanh không hợp lệ.",
                    nameof(file));
            }

            // S7: Chỉ chấp nhận file MP4.
            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".mp4",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Chỉ chấp nhận file MP4.",
                    nameof(file));
            }

            // S7: MIME type phải đúng MP4.
            if (!string.Equals(
                file.ContentType,
                "video/mp4",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Content-Type phải là video/mp4.",
                    nameof(file));
            }

            // S7: Không vượt quá 500 MB.
            if (file.Length > MaxUploadSizeBytes)
            {
                throw new ArgumentException(
                    "File MP4 không được vượt quá 500 MB.",
                    nameof(file));
            }

            var apiKey = _configuration["GeminiSettings:ApiKey"];

            var model =
                _configuration["GeminiSettings:Model"]
                ?? "gemini-1.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Thiếu GeminiSettings:ApiKey trong appsettings.json.");
            }

            var seminar =
                await _seminarRepository.GetByIdAsync(seminarId);

            if (seminar == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy Seminar với ID = {seminarId}");
            }

            string tempInputPath = string.Empty;
            string tempCompressedPath = string.Empty;
            string? resourceName = null;

            try
            {
                // 1. Lưu file MP4 tạm thời.
                tempInputPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid()}.mp4");

                await using (var stream =
                    new FileStream(
                        tempInputPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                {
                    await file.CopyToAsync(
                        stream,
                        cancellationToken);
                }

                // 2. S7: Kiểm tra duration thật bằng ffprobe.
                var durationSeconds =
                    await GetVideoDurationSecondsAsync(
                        tempInputPath,
                        cancellationToken);

                // Ticket yêu cầu strictly under 2 hours.
                if (durationSeconds >= MaxVideoDurationSeconds)
                {
                    throw new ArgumentException(
                        "Video phải có thời lượng dưới 2 giờ.",
                        nameof(file));
                }

                // 3. Tách/nén audio từ MP4 thành MP3 bằng FFmpeg.
                tempCompressedPath =
                    await CompressAudioAsync(
                        tempInputPath,
                        cancellationToken);

                // 4. Upload MP3 lên Google Files API.
                var (fileUri, uploadedResourceName) =
                    await UploadToGoogleFilesApiAsync(
                        tempCompressedPath,
                        apiKey,
                        cancellationToken);

                resourceName = uploadedResourceName;

                // 5. Chờ Google xử lý file.
                await WaitForFileActiveAsync(
                    resourceName,
                    apiKey,
                    cancellationToken);

                // 6. Phân tích audio bằng Gemini.
                var summaryMarkdown =
                    await GenerateSummaryTextAsync(
                        fileUri,
                        apiKey,
                        model,
                        cancellationToken);

                // 7. Giữ nguyên flow cũ:
                // persist AiSummary vào Seminar.
                seminar.AiSummary = summaryMarkdown;

                _seminarRepository.Update(seminar);

                await _seminarRepository.SaveChangesAsync();

                return new SeminarAudioSummaryResponse
                {
                    SeminarId = seminar.SeminarId,
                    AiSummary = seminar.AiSummary ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            finally
            {
                // Luôn cleanup local temp files.
                DeleteLocalFile(tempInputPath);
                DeleteLocalFile(tempCompressedPath);

                // Cleanup Google file không chặn response chính.
                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    _ = DeleteGoogleFileAsync(
                        resourceName,
                        apiKey);
                }
            }
        }

        private async Task<double> GetVideoDurationSecondsAsync(
            string inputPath,
            CancellationToken cancellationToken)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments =
                    $"-v error -show_entries format=duration " +
                    $"-of default=noprint_wrappers=1:nokey=1 " +
                    $"\"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process =
                new Process
                {
                    StartInfo = processInfo
                };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không thể chạy ffprobe để kiểm tra thời lượng video. " +
                    "Hãy kiểm tra ffprobe/FFmpeg đã được cài đặt và có trong PATH.",
                    ex);
            }

            var outputTask =
                process.StandardOutput.ReadToEndAsync(cancellationToken);

            var errorTask =
                process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();

            if (process.ExitCode != 0)
            {
                throw new ArgumentException(
                    string.IsNullOrWhiteSpace(error)
                        ? "Không thể đọc thời lượng file MP4."
                        : $"Không thể đọc thời lượng file MP4: {error}");
            }

            if (!double.TryParse(
                output,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var durationSeconds))
            {
                throw new ArgumentException(
                    "Không thể xác định thời lượng file MP4.");
            }

            if (durationSeconds <= 0)
            {
                throw new ArgumentException(
                    "File MP4 có thời lượng không hợp lệ.");
            }

            return durationSeconds;
        }

        private async Task<string> CompressAudioAsync(
            string inputPath,
            CancellationToken cancellationToken)
        {
            var outputPath =
                Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid()}.mp3");

            var processInfo =
                new ProcessStartInfo
                {
                    FileName = "ffmpeg",

                    Arguments =
                        $"-i \"{inputPath}\" " +
                        "-vn " +
                        "-acodec libmp3lame " +
                        "-ab 32k " +
                        "-ar 16000 " +
                        "-ac 1 " +
                        $"-y \"{outputPath}\"",

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            using var process =
                new Process
                {
                    StartInfo = processInfo
                };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không thể chạy FFmpeg. " +
                    "Hãy kiểm tra FFmpeg đã được cài đặt và có trong PATH.",
                    ex);
            }

            var outputTask =
                process.StandardOutput.ReadToEndAsync(cancellationToken);

            var errorTask =
                process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            _ = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0
                || !File.Exists(outputPath))
            {
                DeleteLocalFile(outputPath);

                throw new InvalidOperationException(
                    $"Lỗi nén FFmpeg (Code {process.ExitCode}): {error}");
            }

            return outputPath;
        }

        private async Task<(string FileUri, string ResourceName)>
            UploadToGoogleFilesApiAsync(
                string filePath,
                string apiKey,
                CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);

            var initUrl =
                $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";

            using var initRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    initUrl);

            initRequest.Headers.Add(
                "X-Goog-Upload-Protocol",
                "resumable");

            initRequest.Headers.Add(
                "X-Goog-Upload-Command",
                "start");

            initRequest.Headers.Add(
                "X-Goog-Upload-Header-Content-Length",
                fileInfo.Length.ToString(
                    CultureInfo.InvariantCulture));

            initRequest.Headers.Add(
                "X-Goog-Upload-Header-Content-Type",
                "audio/mp3");

            var metadata =
                new
                {
                    file = new
                    {
                        display_name =
                            Path.GetFileName(filePath)
                    }
                };

            initRequest.Content =
                new StringContent(
                    JsonSerializer.Serialize(
                        metadata,
                        JsonOptions),
                    Encoding.UTF8,
                    "application/json");

            using var initResponse =
                await _httpClient.SendAsync(
                    initRequest,
                    cancellationToken);

            initResponse.EnsureSuccessStatusCode();

            if (!initResponse.Headers.TryGetValues(
                "X-Goog-Upload-URL",
                out var uploadUrls))
            {
                throw new HttpRequestException(
                    "Google Files API không trả về X-Goog-Upload-URL.");
            }

            var uploadUrl =
                uploadUrls.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                throw new HttpRequestException(
                    "Google Files API trả về upload URL không hợp lệ.");
            }

            await using var fileStream =
                File.OpenRead(filePath);

            using var uploadRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    uploadUrl);

            uploadRequest.Headers.Add(
                "X-Goog-Upload-Command",
                "upload, finalize");

            uploadRequest.Headers.Add(
                "X-Goog-Upload-Offset",
                "0");

            uploadRequest.Content =
                new StreamContent(fileStream);

            uploadRequest.Content.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "audio/mp3");

            uploadRequest.Content.Headers.ContentLength =
                fileInfo.Length;

            using var uploadResponse =
                await _httpClient.SendAsync(
                    uploadRequest,
                    cancellationToken);

            uploadResponse.EnsureSuccessStatusCode();

            var responseJson =
                await uploadResponse.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            using var document =
                JsonDocument.Parse(
                    responseJson);

            if (!document.RootElement.TryGetProperty(
                "file",
                out var fileElement))
            {
                throw new HttpRequestException(
                    "Google Files API response không chứa thông tin file.");
            }

            if (!fileElement.TryGetProperty(
                "uri",
                out var uriElement)
                || !fileElement.TryGetProperty(
                    "name",
                    out var nameElement))
            {
                throw new HttpRequestException(
                    "Google Files API response thiếu uri hoặc name.");
            }

            var fileUri =
                uriElement.GetString();

            var resourceName =
                nameElement.GetString();

            if (string.IsNullOrWhiteSpace(fileUri)
                || string.IsNullOrWhiteSpace(resourceName))
            {
                throw new HttpRequestException(
                    "Google Files API trả về thông tin file không hợp lệ.");
            }

            return (
                fileUri,
                resourceName);
        }

        private async Task WaitForFileActiveAsync(
            string resourceName,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var url =
                $"https://generativelanguage.googleapis.com/v1beta/{resourceName}?key={apiKey}";

            for (var attempt = 0; attempt < 60; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var response =
                    await _httpClient.GetAsync(
                        url,
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson =
                        await response.Content
                            .ReadAsStringAsync(
                                cancellationToken);

                    using var document =
                        JsonDocument.Parse(
                            responseJson);

                    if (document.RootElement.TryGetProperty(
                        "state",
                        out var stateElement))
                    {
                        var state =
                            stateElement.GetString();

                        if (string.Equals(
                            state,
                            "ACTIVE",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        if (string.Equals(
                            state,
                            "FAILED",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "Google Files API không thể xử lý file audio.");
                        }
                    }
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    cancellationToken);
            }

            throw new TimeoutException(
                "Hệ thống Google xử lý file âm thanh quá lâu.");
        }

        private async Task<string> GenerateSummaryTextAsync(
            string fileUri,
            string apiKey,
            string model,
            CancellationToken cancellationToken)
        {
            // Giữ nguyên hướng fallback model của flow AI Summary cũ.
            var cleanModel =
                model.Replace(
                    "models/",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Trim();

            var modelsToTry =
                new[]
                {
                    cleanModel,
                    "gemini-flash-latest",
                    "gemini-1.5-flash-latest",
                    "gemini-1.5-pro-latest"
                }
                .Where(m =>
                    !string.IsNullOrWhiteSpace(m))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            const string detailedAcademicPrompt = @"
Bạn là một chuyên gia phân tích dữ liệu âm thanh, tóm tắt hội thảo khoa học và cuộc họp chuyên sâu.
Nhiệm vụ của bạn là nghe kỹ toàn bộ file ghi âm và lập bản phân tích CỰC KỲ CHI TIẾT dạng Markdown. Cấu trúc yêu cầu như sau:

# 1. TỔNG QUAN CHUNG
- **Chủ đề chính:** [Chủ đề bài giảng/hội thảo]
- **Người chủ trì/Diễn giả:** [Tên hoặc Vai trò nếu nhận diện được]
- **Thành phần tham gia:** [Tên các nhân vật xuất hiện trong âm thanh]

# 2. DIỄN BIẾN THEO TIMELINE CHI TIẾT (NGƯỜI NÓI & NỘI DUNG TRAO ĐỔI)
Yêu cầu: Lập bảng thời gian bám sát từng mốc phát biểu. Xác định rõ **Ai phát biểu**, **Nói với ai** và **Nội dung chi tiết là gì**.

| Mốc thời gian | Người phát biểu | Trao đổi / Hướng tới ai | Nội dung trao đổi & Tranh luận chi tiết |
|---|---|---|---|
| [phút:giây] | [Tên/Vai trò A] | [Người B / Toàn thể] | [Mô tả chi tiết ý kiến, luận điểm, con số, lập luận...] |

# 3. NỘI DUNG CHUYÊN MÔN & LÝ THUYẾT HỌC THUẬT
- **Cơ sở lý thuyết / Phương pháp:** [Các khái niệm, phương pháp nghiên cứu được đề cập]
- **Số liệu / Kết quả quan trọng:** [Các con số, báo cáo, dữ liệu cụ thể]

# 4. CHI TIẾT THẢO LUẬN & HỎI ĐÁP (Q&A / DIỄN BIẾN TRANH LUẬN)
Chi tiết từng lượt tương tác qua lại:
- **Lượt trao đổi 1:**
  - **Người hỏi / Đặt vấn đề:** [Tên/Vai trò]
  - **Người trả lời / Phản hồi:** [Tên/Vai trò]
  - **Vấn đề đặt ra:** [Mô tả chi tiết câu hỏi hoặc ý kiến phản biện]
  - **Nội dung giải đáp:** [Mô tả chi tiết câu trả lời hoặc giải pháp đưa ra]

# 5. KẾT LUẬN & DỰ ÁN / ACTION ITEMS
- **Kết luận chung:**
- **Nhiệm vụ & Công việc tiếp theo:**

| Công việc cần làm | Người phụ trách | Đối tượng phối hợp | Hạn chót / Ghi chú |
|---|---|---|---|

*Lưu ý quan trọng:* Tuyệt đối không viết tóm tắt chung chung. Phải tách rõ từng khoảng thời gian và phân định chính xác ai đang nói chuyện với ai.";

            var requestBody =
                new
                {
                    contents =
                        new[]
                        {
                            new
                            {
                                parts =
                                    new object[]
                                    {
                                        new
                                        {
                                            fileData =
                                                new
                                                {
                                                    mimeType = "audio/mp3",
                                                    fileUri
                                                }
                                        },
                                        new
                                        {
                                            text = detailedAcademicPrompt
                                        }
                                    }
                            }
                        }
                };

            foreach (var currentModel in modelsToTry)
            {
                var url =
                    $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={apiKey}";

                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var content =
                        new StringContent(
                            JsonSerializer.Serialize(
                                requestBody,
                                JsonOptions),
                            Encoding.UTF8,
                            "application/json");

                    using var response =
                        await _httpClient.PostAsync(
                            url,
                            content,
                            cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson =
                            await response.Content
                                .ReadAsStringAsync(
                                    cancellationToken);

                        using var document =
                            JsonDocument.Parse(
                                responseJson);

                        if (!document.RootElement.TryGetProperty(
                            "candidates",
                            out var candidates)
                            || candidates.GetArrayLength() == 0)
                        {
                            throw new HttpRequestException(
                                "Gemini API không trả về nội dung tóm tắt.");
                        }

                        var candidate =
                            candidates[0];

                        if (!candidate.TryGetProperty(
                            "content",
                            out var candidateContent)
                            || !candidateContent.TryGetProperty(
                                "parts",
                                out var parts)
                            || parts.GetArrayLength() == 0
                            || !parts[0].TryGetProperty(
                                "text",
                                out var textElement))
                        {
                            throw new HttpRequestException(
                                "Gemini API trả về response không hợp lệ.");
                        }

                        return textElement.GetString()
                            ?? string.Empty;
                    }

                    var statusCode =
                        (int)response.StatusCode;

                    if (statusCode == 404)
                    {
                        _logger.LogWarning(
                            "Model '{Model}' không tồn tại hoặc endpoint không hợp lệ (404). Chuyển model...",
                            currentModel);

                        break;
                    }

                    if ((statusCode == 503
                            || statusCode == 429)
                        && attempt < 3)
                    {
                        var delaySeconds =
                            attempt * 4;

                        _logger.LogWarning(
                            "Model '{Model}' nghẽn tải ({StatusCode}). " +
                            "Thử lại lần {Attempt}/3 sau {Delay}s...",
                            currentModel,
                            statusCode,
                            attempt,
                            delaySeconds);

                        await Task.Delay(
                            TimeSpan.FromSeconds(delaySeconds),
                            cancellationToken);

                        continue;
                    }

                    var errorBody =
                        await response.Content
                            .ReadAsStringAsync(
                                cancellationToken);

                    _logger.LogWarning(
                        "Gemini model '{Model}' trả lỗi {StatusCode}. Response: {Response}",
                        currentModel,
                        statusCode,
                        errorBody);

                    break;
                }
            }

            throw new HttpRequestException(
                "Không thể tạo bản tóm tắt từ Gemini API sau khi đã thử các model khả dụng.");
        }

        private async Task DeleteGoogleFileAsync(
            string resourceName,
            string apiKey)
        {
            try
            {
                var url =
                    $"https://generativelanguage.googleapis.com/v1beta/{resourceName}?key={apiKey}";

                using var response =
                    await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Không thể xóa temporary Google file {ResourceName}. StatusCode: {StatusCode}",
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

        private static void DeleteLocalFile(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path)
                || !File.Exists(path))
            {
                return;
            }

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