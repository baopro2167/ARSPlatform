using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Lecturer")]
    public class SeminarParticipantController : ControllerBase
    {
        private readonly ISeminarParticipantRepository _repository;
        private readonly ISeminarRepository _seminarRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public SeminarParticipantController(
            ISeminarParticipantRepository repository,
            ISeminarRepository seminarRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _repository = repository;
            _seminarRepository = seminarRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var items = await _repository.GetAllForOrganizerWithUserAsync(organizerId);
            var response = _mapper.Map<IEnumerable<SeminarParticipantResponse>>(items);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SeminarParticipantCreateRequest request)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            if (!request.SeminarId.HasValue)
            {
                return BadRequest(new { message = "SeminarId is required." });
            }

            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(
                request.SeminarId.Value);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                return NotFound();
            }

            User? user = null;
            var invitedEmail = request.InvitedEmail?.Trim();

            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value);

                if (user == null)
                {
                    return BadRequest(new { message = "UserId does not exist." });
                }

                invitedEmail = user.Email;
            }
            else if (!string.IsNullOrWhiteSpace(invitedEmail))
            {
                var validator = new EmailAddressAttribute();

                if (!validator.IsValid(invitedEmail))
                {
                    return BadRequest(new { message = "InvitedEmail is invalid." });
                }

                user = await _userRepository.GetByEmailAsync(invitedEmail);

                if (user != null)
                {
                    invitedEmail = user.Email;
                }
            }
            else
            {
                return BadRequest(new
                {
                    message = "UserId or InvitedEmail is required."
                });
            }

            if (seminar.MaxParticipants.HasValue
                && seminar.MaxParticipants.Value > 0
                && seminar.SeminarParticipants.Count >= seminar.MaxParticipants.Value)
            {
                return Conflict(new
                {
                    message = "Seminar has reached MaxParticipants."
                });
            }

            var duplicate = seminar.SeminarParticipants.Any(p =>
                (user != null && p.UserId == user.UserId)
                || (!string.IsNullOrWhiteSpace(invitedEmail)
                    && string.Equals(
                        p.InvitedEmail,
                        invitedEmail,
                        StringComparison.OrdinalIgnoreCase)));

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Participant is already registered for this seminar."
                });
            }

            string invitationStatus;

            try
            {
                invitationStatus = NormalizeParticipantStatus(
                    request.InvitationStatus ?? "INVITED");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (!string.IsNullOrWhiteSpace(request.ParticipantEvaluation))
            {
                invitationStatus = "SUBMITTED";
            }

            var item = new SeminarParticipant
            {
                SeminarId = seminar.SeminarId,
                UserId = user?.UserId,
                InvitedEmail = invitedEmail,
                InvitationStatus = invitationStatus,
                ParticipantEvaluation = request.ParticipantEvaluation
            };

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithSeminarAndUserAsync(
                item.SeminarParticipantId);

            var response = _mapper.Map<SeminarParticipantResponse>(created ?? item);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);

            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return NotFound();
            }

            var response = _mapper.Map<SeminarParticipantResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SeminarParticipantUpdateRequest request)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);

            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return NotFound();
            }

            if (request.InvitationStatus != null)
            {
                try
                {
                    item.InvitationStatus = NormalizeParticipantStatus(
                        request.InvitationStatus);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            if (request.ParticipantEvaluation != null)
            {
                item.ParticipantEvaluation = request.ParticipantEvaluation;

                if (!string.IsNullOrWhiteSpace(request.ParticipantEvaluation))
                {
                    item.InvitationStatus = "SUBMITTED";
                }
            }

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var response = _mapper.Map<SeminarParticipantResponse>(item);
            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);

            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return NotFound();
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            return Ok(new { Message = "Deleted successfully." });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out userId);
        }

        private static string NormalizeParticipantStatus(string status)
        {
            var value = status.Trim().ToLowerInvariant();

            if (value == "pending")
            {
                return "PENDING";
            }

            if (value is "invited" or "accepted" or "confirmed")
            {
                return "INVITED";
            }

            if (value is "submitted" or "complete" or "completed")
            {
                return "SUBMITTED";
            }

            if (value is "declined" or "rejected")
            {
                return "DECLINED";
            }

            throw new ArgumentException(
                "InvitationStatus must be PENDING, INVITED, SUBMITTED, or DECLINED.");
        }
    }
}