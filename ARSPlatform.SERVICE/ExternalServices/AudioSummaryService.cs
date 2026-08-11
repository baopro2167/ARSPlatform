using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.AspNetCore.Http;
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
    public class AudioSummaryService : IAudioSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AudioSummaryService> _logger;
        private readonly ISeminarRepository _seminarRepository;

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

        public async Task<SeminarAudioSummaryResponse> SummarizeSeminarAudioAsync(int seminarId, SeminarAudioSummaryRequest request, CancellationToken cancellationToken = default)
        {
            var file = request.AudioFile;
            if (file == null || file.Length == 0)
                throw new ArgumentException("File âm thanh không hợp lệ.", nameof(file));

            var apiKey = _configuration["GeminiSettings:ApiKey"];
            var model = _configuration["GeminiSettings:Model"] ?? "gemini-1.5-flash";

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Thiếu GeminiSettings:ApiKey trong appsettings.json.");

            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            if (seminar == null)
                throw new KeyNotFoundException($"Không tìm thấy Seminar với ID = {seminarId}");

            string tempInputPath = string.Empty;
            string tempCompressedPath = string.Empty;
            string? resourceName = null;

            try
            {
                // 1. Lưu file tạm thời
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                await using (var stream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                // 2. Nén file bằng FFmpeg về mp3 32k mono (Tối ưu cho file dài 3-4 tiếng)
                tempCompressedPath = await CompressAudioAsync(tempInputPath, cancellationToken);

                // 3. Upload file lên Google Files API
                var (fileUri, resName) = await UploadToGoogleFilesApiAsync(tempCompressedPath, apiKey, cancellationToken);
                resourceName = resName;

                // 4. Chờ Google xử lý xong file audio
                await WaitForFileActiveAsync(resourceName, apiKey, cancellationToken);

                // 5. Gửi Prompt phân tích Timeline/Người nói với cơ chế Retry & Fallback
                string summaryMarkdown = await GenerateSummaryTextAsync(fileUri, apiKey, model, cancellationToken);

                // 6. Lưu kết quả vào CSDL
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
                DeleteLocalFile(tempInputPath);
                DeleteLocalFile(tempCompressedPath);
                if (!string.IsNullOrEmpty(resourceName))
                {
                    _ = DeleteGoogleFileAsync(resourceName, apiKey);
                }
            }
        }

        private async Task<string> CompressAudioAsync(string inputPath, CancellationToken cancellationToken)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputPath}\" -vn -acodec libmp3lame -ab 32k -ar 16000 -ac 1 -y \"{outputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                throw new InvalidOperationException($"Lỗi nén FFmpeg (Code {process.ExitCode}): {error}");
            }

            return outputPath;
        }

        private async Task<(string FileUri, string ResourceName)> UploadToGoogleFilesApiAsync(string filePath, string apiKey, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            string initUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";

            using var initReq = new HttpRequestMessage(HttpMethod.Post, initUrl);
            initReq.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            initReq.Headers.Add("X-Goog-Upload-Command", "start");
            initReq.Headers.Add("X-Goog-Upload-Header-Content-Length", fileInfo.Length.ToString());
            initReq.Headers.Add("X-Goog-Upload-Header-Content-Type", "audio/mp3");
            initReq.Content = new StringContent(JsonSerializer.Serialize(new { file = new { display_name = Path.GetFileName(filePath) } }), Encoding.UTF8, "application/json");

            using var initRes = await _httpClient.SendAsync(initReq, cancellationToken);
            initRes.EnsureSuccessStatusCode();

            string uploadUrl = initRes.Headers.GetValues("X-Goog-Upload-URL").First();

            await using var fs = File.OpenRead(filePath);
            using var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            uploadReq.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadReq.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadReq.Content = new StreamContent(fs);
            uploadReq.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mp3");

            using var uploadRes = await _httpClient.SendAsync(uploadReq, cancellationToken);
            uploadRes.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await uploadRes.Content.ReadAsStringAsync(cancellationToken));
            var fileProp = doc.RootElement.GetProperty("file");
            return (fileProp.GetProperty("uri").GetString()!, fileProp.GetProperty("name").GetString()!);
        }

        private async Task WaitForFileActiveAsync(string resourceName, string apiKey, CancellationToken cancellationToken)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/{resourceName}?key={apiKey}";
            for (int i = 0; i < 60; i++)
            {
                using var res = await _httpClient.GetAsync(url, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
                    if (doc.RootElement.TryGetProperty("state", out var state) && state.GetString() == "ACTIVE")
                        return;
                }
                await Task.Delay(2000, cancellationToken);
            }
            throw new TimeoutException("Hệ thống Google xử lý file âm thanh quá lâu.");
        }

        private async Task<string> GenerateSummaryTextAsync(string fileUri, string apiKey, string model, CancellationToken cancellationToken)
        {
            // Chuẩn hóa danh sách các model alias hỗ trợ REST API v1beta
            var cleanModel = model.Replace("models/", "").Trim();
            var modelsToTry = new[]
            {
        cleanModel,
        "gemini-flash-latest",
        "gemini-1.5-flash-latest",
        "gemini-1.5-pro-latest"
    }.Distinct().Where(m => !string.IsNullOrEmpty(m)).ToList();

            string detailedAcademicPrompt = @"
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

            var body = new
            {
                contents = new[]
                {
            new
            {
                parts = new object[]
                {
                    new { fileData = new { mimeType = "audio/mp3", fileUri } },
                    new { text = detailedAcademicPrompt }
                }
            }
        }
            };

            foreach (var currentModel in modelsToTry)
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={apiKey}";

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    using var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
                    using var res = await _httpClient.PostAsync(url, content, cancellationToken);

                    if (res.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
                        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
                    }

                    int statusCode = (int)res.StatusCode;

                    // Nếu 404 (Sai tên endpoint/model) -> Bỏ qua model này ngay lập tức
                    if (statusCode == 404)
                    {
                        _logger.LogWarning("Model '{Model}' không tồn tại hoặc sai URL (404 Not Found). Chuyển model...", currentModel);
                        break;
                    }

                    // Nếu 503 hoặc 429 (Quá tải/Rate limit) -> Chờ tăng dần (4s, 8s) rồi retry
                    if ((statusCode == 503 || statusCode == 429) && attempt < 3)
                    {
                        int delaySeconds = attempt * 4;
                        _logger.LogWarning("Model '{Model}' nghẽn tải ({StatusCode}). Thử lại lần {Attempt}/3 sau {Delay}s...", currentModel, statusCode, attempt, delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                        continue;
                    }
                }
            }

            throw new HttpRequestException("Toàn bộ endpoint Gemini API thử nghiệm đều quá tải (503). Vui lòng gửi lại request sau ít phút.");
        }

        private async Task DeleteGoogleFileAsync(string resourceName, string apiKey)
        {
            try { await _httpClient.DeleteAsync($"https://generativelanguage.googleapis.com/v1beta/{resourceName}?key={apiKey}"); } catch { }
        }

        private static void DeleteLocalFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                try { File.Delete(path); } catch { }
        }
    }
}