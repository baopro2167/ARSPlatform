using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class SeminarParticipantService : ISeminarParticipantService
    {
        private readonly ISeminarParticipantRepository _repository;
        private readonly ISeminarRepository _seminarRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public SeminarParticipantService(
            ISeminarParticipantRepository repository,
            ISeminarRepository seminarRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _seminarRepository = seminarRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SeminarParticipantResponse>> GetAllForOrganizerAsync(int organizerId)
        {
            var items = await _repository.GetAllForOrganizerWithUserAsync(organizerId);
            return _mapper.Map<IEnumerable<SeminarParticipantResponse>>(items);
        }

        public async Task<PagedResult<SeminarParticipantResponse>> GetPagedForOrganizerAsync(PaginationParams paginationParams, int organizerId, int? seminarId = null)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: x => x.Seminar != null && x.Seminar.OrganizerId == organizerId && (!seminarId.HasValue || x.SeminarId == seminarId.Value),
                includes: new System.Linq.Expressions.Expression<Func<SeminarParticipant, object>>[]
                {
                    x => x.Seminar!,
                    x => x.User!
                });

            var dtos = _mapper.Map<List<SeminarParticipantResponse>>(paged.Items);
            return new PagedResult<SeminarParticipantResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SeminarParticipantResponse>> GetBySeminarIdAsync(int seminarId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetBySeminarIdPagedAsync(seminarId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<SeminarParticipantResponse>>(paged.Items);
            return new PagedResult<SeminarParticipantResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SeminarParticipantResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<SeminarParticipantResponse>>(paged.Items);
            return new PagedResult<SeminarParticipantResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SeminarParticipantResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            var paged = await _repository.GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
            var dtos = _mapper.Map<List<SeminarParticipantResponse>>(paged.Items);
            return new PagedResult<SeminarParticipantResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<SeminarParticipantResponse?> GetByIdAsync(int id, int organizerId)
        {
            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);
            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return null;
            }

            return _mapper.Map<SeminarParticipantResponse>(item);
        }

        public async Task<SeminarParticipantResponse> CreateAsync(SeminarParticipantCreateRequest request, int organizerId)
        {
            if (!request.SeminarId.HasValue)
            {
                throw new ArgumentException("SeminarId is required.");
            }

            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(request.SeminarId.Value);
            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                throw new KeyNotFoundException("Seminar not found.");
            }

            User? user = null;
            var invitedEmail = request.InvitedEmail?.Trim();

            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value);
                if (user == null)
                {
                    throw new ArgumentException("UserId does not exist.");
                }
                invitedEmail = user.Email;
            }
            else if (!string.IsNullOrWhiteSpace(invitedEmail))
            {
                var validator = new EmailAddressAttribute();
                if (!validator.IsValid(invitedEmail))
                {
                    throw new ArgumentException("InvitedEmail is invalid.");
                }

                user = await _userRepository.GetByEmailAsync(invitedEmail);
                if (user != null)
                {
                    invitedEmail = user.Email;
                }
            }
            else
            {
                throw new ArgumentException("UserId or InvitedEmail is required.");
            }

            if (seminar.MaxParticipants.HasValue && seminar.MaxParticipants.Value > 0 && seminar.SeminarParticipants.Count >= seminar.MaxParticipants.Value)
            {
                throw new InvalidOperationException("Seminar has reached MaxParticipants.");
            }

            var duplicate = seminar.SeminarParticipants.Any(p =>
                (user != null && p.UserId == user.UserId) ||
                (!string.IsNullOrWhiteSpace(invitedEmail) && string.Equals(p.InvitedEmail, invitedEmail, StringComparison.OrdinalIgnoreCase)));

            if (duplicate)
            {
                throw new InvalidOperationException("Participant is already registered for this seminar.");
            }

            var invitationStatus = NormalizeParticipantStatus(request.InvitationStatus ?? "INVITED");
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

            if (user?.UserId != null)
            {
                var seminarTitle = !string.IsNullOrWhiteSpace(seminar.Content) ? seminar.Content : "Hội thảo";
                var notification = new Notification
                {
                    UserId = user.UserId,
                    Message = $"Bạn đã nhận được lời mời tham gia Hội thảo khoa học: \"{seminarTitle}\".",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

            var created = await _repository.GetByIdWithSeminarAndUserAsync(item.SeminarParticipantId);
            return _mapper.Map<SeminarParticipantResponse>(created ?? item);
        }

        public async Task<SeminarParticipantResponse?> UpdateAsync(int id, SeminarParticipantUpdateRequest request, int organizerId)
        {
            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);
            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return null;
            }

            if (request.InvitationStatus != null)
            {
                item.InvitationStatus = NormalizeParticipantStatus(request.InvitationStatus);
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

            return _mapper.Map<SeminarParticipantResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id, int organizerId)
        {
            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);
            if (item == null || item.Seminar?.OrganizerId != organizerId)
            {
                return false;
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        private static string NormalizeParticipantStatus(string status)
        {
            var value = status.Trim().ToLowerInvariant();
            if (value == "pending") return "PENDING";
            if (value is "invited" or "accepted" or "confirmed") return "INVITED";
            if (value is "submitted" or "complete" or "completed") return "SUBMITTED";
            if (value is "declined" or "rejected") return "DECLINED";

            throw new ArgumentException("InvitationStatus must be PENDING, INVITED, SUBMITTED, or DECLINED.");
        }
    }
}
