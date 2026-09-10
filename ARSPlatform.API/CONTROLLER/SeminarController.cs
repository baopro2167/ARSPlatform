using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SeminarController : ControllerBase
    {
        private readonly ISeminarService _seminarService;
        private readonly ISeminarParticipantService _participantService;
        private readonly IAudioSummaryService _audioSummaryService;

        public SeminarController(
            ISeminarService seminarService,
            ISeminarParticipantService participantService,
            IAudioSummaryService audioSummaryService)
        {
            _seminarService = seminarService;
            _participantService = participantService;
            _audioSummaryService = audioSummaryService;
        }

        /// <summary>
        /// Lấy danh sách các buổi Seminar do Lecturer/Researcher hiện tại tổ chức
        /// </summary>
        /// <returns>Danh sách Seminar</returns>
        [HttpGet]
        [Authorize(Roles = "Lecturer,Researcher")]
        public async Task<ActionResult<IEnumerable<SeminarResponse>>> GetAll()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetAllAsync(userId);

            foreach (var seminar in response)
            {
                seminar.AiSummary = null;
            }

            return Ok(response);
        }

        /// <summary>
        /// Lấy danh sách các buổi Seminar mà người dùng hiện tại được mời tham dự
        /// </summary>
        /// <returns>Danh sách Seminar được mời</returns>
        [HttpGet("my-invitations")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SeminarInvitationResponse>>> GetMyInvitations()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            var response = await _participantService.GetMyInvitationsAsync(currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Gợi ý danh sách người tham dự phù hợp theo chuyên ngành (SubFieldId) để mời tham gia Seminar
        /// </summary>
        /// <param name="subFieldId">ID chuyên ngành phụ</param>
        /// <returns>Danh sách người dùng / chuyên gia gợi ý</returns>
        [HttpGet("suggested-invitees")]
        [Authorize]
        public async Task<ActionResult<List<SuggestedInviteeDto>>> GetSuggestedInvitees([FromQuery] int subFieldId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            if (subFieldId <= 0)
            {
                return BadRequest(new { message = "SubFieldId không hợp lệ." });
            }

            var result = await _seminarService.GetSuggestedInviteesAsync(subFieldId, currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách Seminar có phân trang</returns>
        [HttpGet("paged")]
        [Authorize(Roles = "Lecturer,Researcher")]
        public async Task<ActionResult<PagedResult<SeminarResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _seminarService.GetPagedAsync(
                paginationParams,
                userId);

            foreach (var seminar in result.Items)
            {
                seminar.AiSummary = null;
            }

            return Ok(result);
        }

        /// <summary>
        /// Tạo mới buổi Seminar trực tuyến (tự động tích hợp Google Meet và Google Calendar)
        /// </summary>
        /// <param name="request">Thông tin buổi Seminar</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Buổi Seminar vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "Lecturer,Researcher")]
        public async Task<ActionResult<SeminarResponse>> Create(
            [FromBody] SeminarCreateRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.CreateAsync(
                    organizerId,
                    request,
                    cancellationToken);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "Failed to generate Google Meet link.", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết buổi Seminar theo ID
        /// </summary>
        /// <param name="id">ID buổi Seminar</param>
        /// <returns>Chi tiết Seminar</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<SeminarResponse>> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetByIdForViewerAsync(
                id,
                userId);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        /// <summary>
        /// Lấy danh sách feedback của người tham dự trong Seminar
        /// </summary>
        /// <param name="id">ID buổi Seminar</param>
        /// <returns>Danh sách người tham dự kèm điểm đánh giá và nội dung feedback</returns>
        [HttpGet("{id:int}/feedback")]
        [Authorize(Roles = "Lecturer,Researcher,Admin")]
        [ProducesResponseType(typeof(IEnumerable<SeminarParticipantResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SeminarParticipantResponse>>> GetFeedback(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var response = await _participantService.GetFeedbackBySeminarIdAsync(id, organizerId, IsAdmin());
            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        /// <summary>
        /// Cấu hình danh sách câu hỏi khảo sát động cho buổi Seminar
        /// </summary>
        /// <param name="id">ID buổi Seminar</param>
        /// <param name="payload">Payload chứa câu hỏi (JSON string, object, hoặc array)</param>
        /// <returns>Cấu hình câu hỏi đã lưu</returns>
        [HttpPut("{id:int}/feedback-form")]
        [HttpPost("{id:int}/feedback-form")]
        [Authorize(Roles = "Lecturer,Researcher,Admin")]
        [ProducesResponseType(typeof(SeminarFeedbackFormResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SeminarFeedbackFormResponse>> UpdateFeedbackForm(int id, [FromBody] JsonElement payload)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.UpdateFeedbackFormAsync(id, organizerId, payload, IsAdmin());
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi khảo sát động của buổi Seminar
        /// </summary>
        /// <param name="id">ID buổi Seminar</param>
        /// <returns>Cấu hình câu hỏi của Seminar</returns>
        [HttpGet("{id:int}/feedback-form")]
        [Authorize]
        [ProducesResponseType(typeof(SeminarFeedbackFormResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SeminarFeedbackFormResponse>> GetFeedbackForm(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetFeedbackFormAsync(id, currentUserId, IsAdmin());
            if (response == null)
            {
                return NotFound(new { message = $"Seminar with ID {id} not found or access denied." });
            }

            return Ok(response);
        }

        /// <summary>
        /// Nộp form đánh giá / feedback động cho buổi Seminar
        /// </summary>
        /// <param name="id">ID buổi Seminar</param>
        /// <param name="rawBody">Nội dung đánh giá động (hoặc cấu trúc feedback cũ)</param>
        /// <returns>Kết quả nộp đánh giá</returns>
        [HttpPost("{id:int}/feedback")]
        [HttpPost("{id:int}/feedback-answers")]
        [Authorize]
        public async Task<ActionResult<SeminarFeedbackResponse>> SubmitFeedback(int id, [FromBody] JsonElement rawBody)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            var request = ConvertToFeedbackRequest(rawBody);

            try
            {
                var response = await _participantService.SubmitFeedbackAsync(id, request, currentUserId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin buổi Seminar
        /// </summary>
        /// <param name="id">ID Seminar cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Seminar sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Lecturer,Researcher,Admin")]
        public async Task<ActionResult<SeminarResponse>> Update(
            int id,
            [FromBody] SeminarUpdateRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.UpdateAsync(
                    id,
                    organizerId,
                    request,
                    cancellationToken,
                    IsAdmin());

                if (response == null)
                {
                    return NotFound();
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Hủy / Xóa buổi Seminar
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Lecturer,Researcher")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var deleted = await _seminarService.DeleteAsync(id, organizerId);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok(new { Message = "Deleted successfully." });
        }

        /// <summary>
        /// Gửi lời mời tham gia Seminar cho người tham dự
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <param name="request">Danh sách email hoặc user ID cần mời</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Kết quả gửi lời mời</returns>
        [HttpPost("{id:int}/invite")]
        [Authorize(Roles = "Lecturer,Researcher")]
        [ProducesResponseType(typeof(SeminarInviteResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Invite(
            int id,
            [FromBody] SeminarInviteRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.InviteAsync(
                    id,
                    organizerId,
                    request,
                    cancellationToken);

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê về số lượng tham gia và phản hồi của Seminar
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <returns>Thống kê số liệu</returns>
        [HttpGet("{id:int}/stats")]
        [Authorize(Roles = "Lecturer,Researcher")]
        [ProducesResponseType(typeof(SeminarStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetStatsAsync(id, organizerId);
            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        /// <summary>
        /// Gửi email nhắc nhở người tham gia điền đánh giá / phản hồi Seminar
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Kết quả gửi nhắc nhở</returns>
        [HttpPost("{id:int}/reminders/send")]
        [Authorize(Roles = "Lecturer,Researcher")]
        [ProducesResponseType(typeof(SeminarReminderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendFeedbackReminders(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.SendFeedbackRemindersAsync(
                    id,
                    organizerId,
                    cancellationToken);

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Tổng hợp toàn bộ feedback của người tham dự bằng Gemini AI và lưu kết quả vào Seminar
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Kết quả AI tổng hợp feedback</returns>
        [HttpPost("{id:int}/summarize-feedback")]
        [Authorize(Roles = "Lecturer,Researcher")]
        [ProducesResponseType(typeof(SeminarFeedbackAiSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SummarizeFeedback(int id, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _seminarService.SummarizeFeedbackAsync(id, organizerId, cancellationToken);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tải lên file ghi âm buổi Seminar và tự động tóm tắt bằng Gemini AI
        /// </summary>
        /// <param name="id">ID Seminar</param>
        /// <param name="request">File âm thanh ghi âm</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bản tóm tắt nội dung buổi Seminar từ AI</returns>
        [HttpPost("{id:int}/summarize-audio")]
        [Authorize(Roles = "Lecturer,Researcher")]
        [RequestSizeLimit(524_288_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        [ProducesResponseType(typeof(SeminarAudioSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SummarizeAudio(
            int id,
            [FromForm] SeminarAudioSummaryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var seminar = await _seminarService.GetByIdAsync(
                id,
                organizerId);

            if (seminar == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(seminar.AiSummary)
                && !request.ReplaceExisting)
            {
                return Conflict(new
                {
                    code = "SUMMARY_ALREADY_EXISTS",
                    message = "Seminar đã có AI Summary. Hãy xác nhận nếu muốn thay thế kết quả hiện tại."
                });
            }

            try
            {
                var result = await _audioSummaryService.SummarizeSeminarAudioAsync(
                    id,
                    request,
                    cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = ex.Message });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out userId);
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        internal static SeminarFeedbackRequest ConvertToFeedbackRequest(JsonElement element)
        {
            var request = new SeminarFeedbackRequest();

            if (element.ValueKind == JsonValueKind.Array)
            {
                request.FeedbackJson = element.GetRawText();
                try
                {
                    request.Answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(element.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch { }
                return request;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("feedbackJson", out var fjProp))
                {
                    if (fjProp.ValueKind == JsonValueKind.String)
                    {
                        request.FeedbackJson = fjProp.GetString();
                    }
                    else
                    {
                        request.FeedbackJson = fjProp.GetRawText();
                    }
                }

                if (element.TryGetProperty("answers", out var ansProp) && ansProp.ValueKind == JsonValueKind.Array)
                {
                    try
                    {
                        request.Answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(ansProp.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch { }
                }

                if (element.TryGetProperty("feedback", out var fbProp) && fbProp.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        request.Feedback = JsonSerializer.Deserialize<SeminarFeedbackContentRequest>(fbProp.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch { }
                }

                if (element.TryGetProperty("participantEvaluation", out var peProp) && peProp.ValueKind == JsonValueKind.String)
                {
                    request.ParticipantEvaluation = peProp.GetString();
                }

                if (element.TryGetProperty("invitationStatus", out var isProp) && isProp.ValueKind == JsonValueKind.String)
                {
                    request.InvitationStatus = isProp.GetString();
                }

                return request;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                request.FeedbackJson = element.GetString();
                return request;
            }

            return request;
        }
    }
}