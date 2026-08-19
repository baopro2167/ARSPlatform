using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Lecturer")]
    public class SeminarController : ControllerBase
    {
        private readonly ISeminarService _seminarService;
        private readonly IAudioSummaryService _audioSummaryService;

        public SeminarController(
            ISeminarService seminarService,
            IAudioSummaryService audioSummaryService)
        {
            _seminarService = seminarService;
            _audioSummaryService = audioSummaryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetAllAsync(organizerId);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
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
            catch (HttpRequestException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "Failed to generate Google Meet link." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var response = await _seminarService.GetByIdAsync(id, organizerId);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
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
                    cancellationToken);

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

        [HttpDelete("{id:int}")]
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

        [HttpPost("{id:int}/invite")]
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

        [HttpGet("{id:int}/stats")]
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

        [HttpPost("{id:int}/reminders/send")]
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

        [HttpPost("{id:int}/summarize-audio")]
        [RequestSizeLimit(524_288_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        [ProducesResponseType(typeof(SeminarAudioSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SummarizeAudio(
            int id,
            [FromForm] SeminarAudioSummaryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            if (!await _seminarService.IsOwnedByOrganizerAsync(id, organizerId))
            {
                return NotFound();
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
    }
}