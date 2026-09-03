using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class SeminarParticipantService : ISeminarParticipantService
    {
        private readonly ISeminarParticipantRepository _repository;
        private readonly ISeminarRepository _seminarRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        private static readonly JsonSerializerOptions FeedbackJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SeminarParticipantService(ISeminarParticipantRepository repository, ISeminarRepository seminarRepository, IUserRepository userRepository, INotificationRepository notificationRepository, IMapper mapper)
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

        public async Task<IEnumerable<SeminarParticipantResponse>?> GetFeedbackBySeminarIdAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            if (seminar == null || seminar.OrganizerId != organizerId)
                return null;

            var items = await _repository.GetBySeminarIdWithUserAsync(seminarId);
            return _mapper.Map<IEnumerable<SeminarParticipantResponse>>(items);
        }

        public async Task<PagedResult<SeminarParticipantResponse>> GetPagedForOrganizerAsync(PaginationParams paginationParams, int organizerId, int? seminarId = null)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: x => x.Seminar != null && x.Seminar.OrganizerId == organizerId && (!seminarId.HasValue || x.SeminarId == seminarId.Value),
                includes: new System.Linq.Expressions.Expression<Func<SeminarParticipant, object>>[] { x => x.Seminar!, x => x.User! });

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
                return null;

            return _mapper.Map<SeminarParticipantResponse>(item);
        }

        public async Task<SeminarParticipantResponse> CreateAsync(SeminarParticipantCreateRequest request, int organizerId)
        {
            if (!request.SeminarId.HasValue)
                throw new ArgumentException("SeminarId is required.");

            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(request.SeminarId.Value);
            if (seminar == null || seminar.OrganizerId != organizerId)
                throw new KeyNotFoundException("Seminar not found.");

            User? user = null;
            var invitedEmail = request.InvitedEmail?.Trim();

            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value);
                if (user == null)
                    throw new ArgumentException("UserId does not exist.");

                invitedEmail = user.Email;
            }
            else if (!string.IsNullOrWhiteSpace(invitedEmail))
            {
                var validator = new EmailAddressAttribute();
                if (!validator.IsValid(invitedEmail))
                    throw new ArgumentException("InvitedEmail is invalid.");

                user = await _userRepository.GetByEmailAsync(invitedEmail);
                if (user != null)
                    invitedEmail = user.Email;
            }
            else
            {
                throw new ArgumentException("UserId or InvitedEmail is required.");
            }

            if (seminar.MaxParticipants.HasValue && seminar.MaxParticipants.Value > 0 && seminar.SeminarParticipants.Count >= seminar.MaxParticipants.Value)
                throw new InvalidOperationException("Seminar has reached MaxParticipants.");

            var duplicate = seminar.SeminarParticipants.Any(p =>
                (user != null && p.UserId == user.UserId) ||
                (!string.IsNullOrWhiteSpace(invitedEmail) && string.Equals(p.InvitedEmail, invitedEmail, StringComparison.OrdinalIgnoreCase)));

            if (duplicate)
                throw new InvalidOperationException("Participant is already registered for this seminar.");

            if (HasFeedbackPayload(request.Feedback, request.ParticipantEvaluation))
                throw new UnauthorizedAccessException("Seminar owner cannot submit feedback on behalf of a participant.");

            var invitationStatus = NormalizeParticipantStatus(request.InvitationStatus ?? "INVITED");
            var item = new SeminarParticipant
            {
                SeminarId = seminar.SeminarId,
                UserId = user?.UserId,
                InvitedEmail = invitedEmail,
                InvitationStatus = invitationStatus,
                FeedbackJson = null,
                FeedbackSubmittedAt = null,
                FeedbackUpdatedAt = null
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

        public async Task<SeminarParticipantResponse?> UpdateAsync(int id, SeminarParticipantUpdateRequest request, int currentUserId)
        {
            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);
            if (item == null)
                return null;

            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            var isOrganizer = item.Seminar?.OrganizerId == currentUserId;
            var isParticipant = item.UserId == currentUserId ||
                (!string.IsNullOrWhiteSpace(currentUser?.Email) && string.Equals(item.InvitedEmail, currentUser.Email, StringComparison.OrdinalIgnoreCase));

            if (!isOrganizer && !isParticipant)
                return null;

            var hasFeedbackPayload = HasFeedbackPayload(request.Feedback, request.ParticipantEvaluation);
            if (isOrganizer && hasFeedbackPayload)
                throw new UnauthorizedAccessException("Seminar owner cannot submit or edit feedback on behalf of a participant.");

            if (request.InvitationStatus != null)
                item.InvitationStatus = NormalizeParticipantStatus(request.InvitationStatus);

            var submittedFeedback = false;
            if (isParticipant && hasFeedbackPayload)
            {
                var normalizedFeedback = NormalizeFeedback(request.Feedback, request.ParticipantEvaluation);
                var now = DateTime.UtcNow;
                item.FeedbackJson = SerializeFeedback(normalizedFeedback);
                item.FeedbackSubmittedAt ??= now;
                item.FeedbackUpdatedAt = now;
                item.InvitationStatus = "SUBMITTED";
                submittedFeedback = true;
            }

            if (isParticipant && item.UserId == null)
                item.UserId = currentUserId;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            if (isParticipant && submittedFeedback && item.Seminar?.OrganizerId != null)
                await TryCreateFeedbackNotificationAsync(item.Seminar.OrganizerId.Value, currentUser, item.Seminar.Content);

            return _mapper.Map<SeminarParticipantResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id, int organizerId)
        {
            var item = await _repository.GetByIdWithSeminarAndUserAsync(id);
            if (item == null || item.Seminar?.OrganizerId != organizerId)
                return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<SeminarFeedbackResponse> SubmitFeedbackAsync(int seminarId, SeminarFeedbackRequest request, int currentUserId)
        {
            var normalizedFeedback = NormalizeFeedback(request.Feedback, request.ParticipantEvaluation);
            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            if (currentUser == null)
                throw new UnauthorizedAccessException("User not found.");

            var participant = await _repository.GetBySeminarAndUserAsync(seminarId, currentUserId, currentUser.Email);
            if (participant == null)
            {
                var seminarExists = await _seminarRepository.GetByIdAsync(seminarId);
                if (seminarExists == null)
                    throw new KeyNotFoundException($"Seminar with ID {seminarId} not found.");

                throw new InvalidOperationException("You are not registered or invited to this seminar.");
            }

            if (participant.UserId == null)
                participant.UserId = currentUserId;

            var now = DateTime.UtcNow;
            participant.FeedbackJson = SerializeFeedback(normalizedFeedback);
            participant.FeedbackSubmittedAt ??= now;
            participant.FeedbackUpdatedAt = now;
            participant.InvitationStatus = "SUBMITTED";

            _repository.Update(participant);
            await _repository.SaveChangesAsync();

            if (participant.Seminar?.OrganizerId != null)
                await TryCreateFeedbackNotificationAsync(participant.Seminar.OrganizerId.Value, currentUser, participant.Seminar.Content);

            return new SeminarFeedbackResponse
            {
                SeminarId = seminarId,
                SeminarParticipantId = participant.SeminarParticipantId,
                UserId = participant.UserId,
                Feedback = normalizedFeedback,
                ParticipantEvaluation = normalizedFeedback.OverallComment,
                FeedbackSubmittedAt = participant.FeedbackSubmittedAt.Value,
                FeedbackUpdatedAt = participant.FeedbackUpdatedAt,
                InvitationStatus = participant.InvitationStatus ?? "SUBMITTED",
                Message = "Feedback submitted successfully."
            };
        }

        public async Task<IEnumerable<SeminarInvitationResponse>> GetMyInvitationsAsync(int currentUserId)
        {
            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            var list = await _repository.GetMyInvitationsAsync(currentUserId, currentUser?.Email);

            return list.Select(p =>
            {
                var feedback = DeserializeFeedback(p.FeedbackJson);
                return new SeminarInvitationResponse
                {
                    SeminarId = p.SeminarId ?? 0,
                    SeminarParticipantId = p.SeminarParticipantId,
                    Title = p.Seminar?.Content ?? "Seminar",
                    StartTime = p.Seminar?.StartTime ?? DateTime.MinValue,
                    EndTime = p.Seminar?.EndTime ?? DateTime.MinValue,
                    OnlineLink = p.Seminar?.OnlineLink,
                    OrganizerName = p.Seminar?.Organizer?.FullName ?? "Giảng viên",
                    InvitationStatus = p.InvitationStatus,
                    Feedback = feedback,
                    ParticipantEvaluation = feedback?.OverallComment,
                    FeedbackSubmittedAt = p.FeedbackSubmittedAt,
                    FeedbackUpdatedAt = p.FeedbackUpdatedAt
                };
            }).ToList();
        }

        private async Task TryCreateFeedbackNotificationAsync(int organizerId, User? currentUser, string? seminarContent)
        {
            try
            {
                var participantName = !string.IsNullOrWhiteSpace(currentUser?.FullName) ? currentUser.FullName : (currentUser?.Email ?? "Người tham dự");
                var seminarTitle = !string.IsNullOrWhiteSpace(seminarContent)
                    ? (seminarContent.Length > 50 ? seminarContent[..50] + "..." : seminarContent)
                    : "Hội thảo";

                var notification = new Notification
                {
                    UserId = organizerId,
                    Message = $"[Seminar] {participantName} đã gửi phản hồi cho buổi Seminar: \"{seminarTitle}\"",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
            catch
            {
                // Bỏ qua lỗi notification để không ảnh hưởng luồng feedback.
            }
        }

        private static bool HasFeedbackPayload(SeminarFeedbackContentRequest? feedback, string? legacyParticipantEvaluation)
        {
            if (!string.IsNullOrWhiteSpace(legacyParticipantEvaluation))
                return true;

            if (feedback == null)
                return false;

            return !string.IsNullOrWhiteSpace(feedback.OverallComment)
                || feedback.Strengths?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                || feedback.Improvements?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                || feedback.Suggestions?.Any(x => !string.IsNullOrWhiteSpace(x)) == true;
        }

        private static SeminarFeedbackContentResponse NormalizeFeedback(SeminarFeedbackContentRequest? feedback, string? legacyParticipantEvaluation)
        {
            var overallComment = !string.IsNullOrWhiteSpace(feedback?.OverallComment)
                ? feedback.OverallComment.Trim()
                : (!string.IsNullOrWhiteSpace(legacyParticipantEvaluation) ? legacyParticipantEvaluation.Trim() : null);

            var result = new SeminarFeedbackContentResponse
            {
                OverallComment = overallComment,
                Strengths = NormalizeFeedbackItems(feedback?.Strengths),
                Improvements = NormalizeFeedbackItems(feedback?.Improvements),
                Suggestions = NormalizeFeedbackItems(feedback?.Suggestions)
            };

            if (string.IsNullOrWhiteSpace(result.OverallComment) && result.Strengths.Count == 0 && result.Improvements.Count == 0 && result.Suggestions.Count == 0)
                throw new ArgumentException("Feedback must contain at least one overall comment, strength, improvement, or suggestion.");

            return result;
        }

        private static List<string> NormalizeFeedbackItems(IEnumerable<string>? items)
        {
            if (items == null)
                return new List<string>();

            return items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string SerializeFeedback(SeminarFeedbackContentResponse feedback)
        {
            return JsonSerializer.Serialize(feedback, FeedbackJsonOptions);
        }

        private static SeminarFeedbackContentResponse? DeserializeFeedback(string? feedbackJson)
        {
            if (string.IsNullOrWhiteSpace(feedbackJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<SeminarFeedbackContentResponse>(feedbackJson, FeedbackJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
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