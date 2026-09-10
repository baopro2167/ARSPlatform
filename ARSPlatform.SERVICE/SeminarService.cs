using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class SeminarService : ISeminarService
    {
        private static readonly TimeSpan EventReminderWindow = TimeSpan.FromHours(24);

        private readonly ISeminarFeedbackAiService _seminarFeedbackAiService;
        private readonly ISeminarRepository _seminarRepository;
        private readonly ISeminarParticipantRepository _participantRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IGoogleMeetService _googleMeetService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<SeminarService> _logger;

        public SeminarService(
            ISeminarFeedbackAiService seminarFeedbackAiService,
            ISeminarRepository seminarRepository,
            ISeminarParticipantRepository participantRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IGoogleMeetService googleMeetService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<SeminarService> logger)
        {
            _seminarFeedbackAiService = seminarFeedbackAiService;
            _seminarRepository = seminarRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _googleMeetService = googleMeetService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SeminarResponse>> GetAllAsync(int organizerId)
        {
            var seminars = await _seminarRepository.GetAllForOrganizerWithParticipantsAsync(organizerId);
            return _mapper.Map<IEnumerable<SeminarResponse>>(seminars);
        }

        public async Task<PagedResult<SeminarResponse>> GetPagedAsync(PaginationParams paginationParams, int organizerId)
        {
            var paged = await _seminarRepository.GetPagedAsync(
                paginationParams,
                predicate: x => x.OrganizerId == organizerId,
                orderBy: q => q.OrderByDescending(x => x.StartTime),
                includes: x => x.SeminarParticipants);

            var dtos = _mapper.Map<List<SeminarResponse>>(paged.Items);
            return new PagedResult<SeminarResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SeminarResponse>> GetByOrganizerIdAsync(int organizerId, int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize }, organizerId);
        }

        public async Task<SeminarResponse?> GetByIdAsync(int seminarId, int? organizerId = null)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null)
                return null;

            if (organizerId.HasValue && seminar.OrganizerId != organizerId.Value)
                return null;

            return _mapper.Map<SeminarResponse>(seminar);
        }

        public async Task<SeminarResponse?> GetByIdForViewerAsync(int seminarId, int currentUserId)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null)
                return null;

            // Chủ Seminar luôn được xem toàn bộ dữ liệu participant, bao gồm raw feedback.
            if (seminar.OrganizerId == currentUserId)
                return _mapper.Map<SeminarResponse>(seminar);

            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            var currentUserEmail = currentUser?.Email;

            // Participant được nhận diện bằng UserId hoặc email được mời.
            var isParticipant = seminar.SeminarParticipants.Any(participant =>
                NormalizeParticipantStatus(participant.InvitationStatus) != "DECLINED"
                && (participant.UserId == currentUserId
                    || (!string.IsNullOrWhiteSpace(currentUserEmail)
                        && string.Equals(participant.InvitedEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))));

            if (!isParticipant)
                return null;

            var response = _mapper.Map<SeminarResponse>(seminar);

            // Participant chỉ được xem raw feedback của chính mình, không được xem feedback của participant khác.
            foreach (var participant in response.Participants)
            {
                var isCurrentParticipant = participant.UserId == currentUserId
                    || (!string.IsNullOrWhiteSpace(currentUserEmail)
                        && (string.Equals(participant.InvitedEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(participant.UserEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase)));

                if (!isCurrentParticipant)
                {
                    participant.Feedback = null;
                    participant.FeedbackJson = null;
                    participant.ParticipantEvaluation = null;
                    participant.FeedbackSubmittedAt = null;
                    participant.FeedbackUpdatedAt = null;
                }
            }

            return response;
        }

        public async Task<SeminarResponse> CreateAsync(int organizerId, SeminarCreateRequest request, CancellationToken cancellationToken = default)
        {
            ValidateSeminarValues(request.StartTime, request.EndTime, request.Content, request.MaxParticipants, 0);

            var normalizedGuestEmails = NormalizeEmails(request.GuestEmails);

            if (request.MaxParticipants.HasValue
                && request.MaxParticipants.Value > 0
                && normalizedGuestEmails.Count > request.MaxParticipants.Value)
            {
                throw new ArgumentException("Guest email count exceeds MaxParticipants.");
            }

            var googleMeetLink = await _googleMeetService.CreateMeetingSpaceAsync(cancellationToken);
            var seminar = _mapper.Map<Seminar>(request);

            seminar.OrganizerId = organizerId;
            seminar.OnlineLink = googleMeetLink;
            seminar.ReminderEnabled = request.IsReminderSent ?? false;
            seminar.IsReminderSent = false;
            seminar.ReminderSentAt = null;
            seminar.SubFieldId = request.SubFieldId;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var reqStatus = request.Status.Trim();
                seminar.Status = IsDraft(reqStatus)
                    ? "Draft"
                    : (IsInactiveOrSuspended(reqStatus)
                        ? (string.Equals(reqStatus, "Suspended", StringComparison.OrdinalIgnoreCase) ? "Suspended" : "Inactive")
                        : CalculateLifecycleStatus(request.StartTime, request.EndTime, DateTime.UtcNow));
            }
            else
            {
                seminar.Status = CalculateLifecycleStatus(request.StartTime, request.EndTime, DateTime.UtcNow);
            }

            await _seminarRepository.AddAsync(seminar);

            var participants = new List<SeminarParticipant>();
            var notificationsToCreate = new List<Notification>();

            foreach (var email in normalizedGuestEmails)
            {
                var user = await _userRepository.GetByEmailAsync(email);

                var participant = new SeminarParticipant
                {
                    Seminar = seminar,
                    UserId = user?.UserId,
                    InvitedEmail = email,
                    InvitationStatus = "INVITED"
                };

                participants.Add(participant);
                await _participantRepository.AddAsync(participant);

                if (user != null)
                {
                    notificationsToCreate.Add(new Notification
                    {
                        UserId = user.UserId,
                        Message = $"Bạn được mời tham dự hội thảo '{seminar.Content}' diễn ra vào lúc {seminar.StartTime:dd/MM/yyyy HH:mm}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Seminar và participant ban đầu dùng cùng scoped AppDbContext.
            await _seminarRepository.SaveChangesAsync();

            if (notificationsToCreate.Count > 0)
            {
                foreach (var notification in notificationsToCreate)
                    await _notificationRepository.AddAsync(notification);

                await _notificationRepository.SaveChangesAsync();
            }

            if (participants.Count > 0)
            {
                var invitationTimestampChanged = false;

                foreach (var participant in participants)
                {
                    var email = ResolveParticipantEmail(participant);

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    try
                    {
                        await _emailService.SendEmailAsync(email, "[ARS] Seminar Invitation", BuildInvitationEmailBody(seminar));
                        participant.InvitationSentAt = DateTime.UtcNow;
                        invitationTimestampChanged = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send seminar {SeminarId} invitation to {Email}.", seminar.SeminarId, email);
                    }
                }

                if (invitationTimestampChanged)
                    await _participantRepository.SaveChangesAsync();
            }

            var created = await _seminarRepository.GetByIdWithParticipantsAsync(seminar.SeminarId);
            return _mapper.Map<SeminarResponse>(created ?? seminar);
        }

        public async Task<SeminarResponse?> UpdateAsync(int seminarId, int organizerId, SeminarUpdateRequest request, CancellationToken cancellationToken = default, bool isAdmin = false)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || (!isAdmin && seminar.OrganizerId != organizerId))
                return null;

            var startTime = request.StartTime ?? seminar.StartTime;
            var endTime = request.EndTime ?? seminar.EndTime;
            var content = request.Content ?? seminar.Content;
            var maxParticipants = request.MaxParticipants ?? seminar.MaxParticipants;

            ValidateSeminarValues(startTime, endTime, content, maxParticipants, seminar.SeminarParticipants.Count);

            if (request.StartTime.HasValue)
            {
                seminar.StartTime = request.StartTime.Value;
                seminar.IsReminderSent = false;
                seminar.ReminderSentAt = null;

                foreach (var participant in seminar.SeminarParticipants)
                    participant.EventReminderSentAt = null;
            }

            if (request.EndTime.HasValue)
                seminar.EndTime = request.EndTime.Value;

            if (request.Content != null)
                seminar.Content = request.Content.Trim();

            if (request.MaxParticipants.HasValue)
                seminar.MaxParticipants = request.MaxParticipants.Value;

            if (request.SubFieldId.HasValue)
                seminar.SubFieldId = request.SubFieldId.Value;

            if (request.ReminderEnabled.HasValue)
            {
                seminar.ReminderEnabled = request.ReminderEnabled.Value;

                if (!request.ReminderEnabled.Value)
                {
                    seminar.IsReminderSent = false;
                    seminar.ReminderSentAt = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var requestedStatus = request.Status.Trim();
                if (IsInactiveOrSuspended(requestedStatus))
                {
                    seminar.Status = string.Equals(requestedStatus, "Suspended", StringComparison.OrdinalIgnoreCase)
                        ? "Suspended"
                        : "Inactive";
                }
                else if (IsDraft(requestedStatus))
                {
                    seminar.Status = "Draft";
                }
                else if (string.Equals(requestedStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    seminar.Status = CalculateLifecycleStatus(seminar.StartTime, seminar.EndTime, DateTime.UtcNow);
                }
                else if (string.Equals(requestedStatus, "Upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    seminar.Status = "Upcoming";
                }
                else if (string.Equals(requestedStatus, "In Progress", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(requestedStatus, "Ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    seminar.Status = "In Progress";
                }
                else if (string.Equals(requestedStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    seminar.Status = "Completed";
                }
                else
                {
                    seminar.Status = requestedStatus;
                }
            }
            else
            {
                // If request.Status was not specified, only re-calculate if current status is NOT Draft and NOT Inactive/Suspended
                if (!IsDraft(seminar.Status) && !IsInactiveOrSuspended(seminar.Status))
                {
                    seminar.Status = CalculateLifecycleStatus(seminar.StartTime, seminar.EndTime, DateTime.UtcNow);
                }
            }

            _seminarRepository.Update(seminar);
            await _seminarRepository.SaveChangesAsync();

            // FE hiện vẫn dùng PUT { isReminderSent: true } cho Remind Pending.
            // Giữ contract này song song với endpoint reminder riêng.
            if (request.IsReminderSent == true)
                await SendFeedbackRemindersAsync(seminarId, organizerId, cancellationToken);

            var updated = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);
            return _mapper.Map<SeminarResponse>(updated ?? seminar);
        }

        public async Task<bool> DeleteAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
                return false;

            _seminarRepository.Delete(seminar);
            await _seminarRepository.SaveChangesAsync();
            return true;
        }

        public async Task<SeminarInviteResponse> InviteAsync(int seminarId, int organizerId, SeminarInviteRequest request, CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
                throw new KeyNotFoundException("Seminar not found.");

            var normalizedEmails = NormalizeEmails(request.Emails);

            if (normalizedEmails.Count == 0)
                throw new ArgumentException("At least one valid email is required.");

            var response = new SeminarInviteResponse
            {
                SeminarId = seminarId,
                Requested = request.Emails.Count
            };

            var existingParticipants = seminar.SeminarParticipants.ToList();
            var newParticipants = new List<SeminarParticipant>();
            var participantsToSend = new List<SeminarParticipant>();

            foreach (var email in normalizedEmails)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var user = await _userRepository.GetByEmailAsync(email);

                var existing = existingParticipants.FirstOrDefault(p =>
                    (user != null && p.UserId == user.UserId)
                    || string.Equals(p.InvitedEmail, email, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.User?.Email, email, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (existing.InvitationSentAt == null)
                        participantsToSend.Add(existing);
                    else
                        response.Skipped++;

                    continue;
                }

                var participant = new SeminarParticipant
                {
                    SeminarId = seminar.SeminarId,
                    UserId = user?.UserId,
                    InvitedEmail = email,
                    InvitationStatus = "INVITED"
                };

                newParticipants.Add(participant);
                participantsToSend.Add(participant);
            }

            if (seminar.MaxParticipants.HasValue
                && seminar.MaxParticipants.Value > 0
                && existingParticipants.Count + newParticipants.Count > seminar.MaxParticipants.Value)
            {
                throw new InvalidOperationException("Invitations would exceed the seminar MaxParticipants limit.");
            }

            foreach (var participant in newParticipants)
                await _participantRepository.AddAsync(participant);

            if (newParticipants.Count > 0)
            {
                await _participantRepository.SaveChangesAsync();

                var notifications = newParticipants
                    .Where(p => p.UserId.HasValue)
                    .Select(p => new Notification
                    {
                        UserId = p.UserId!.Value,
                        Message = $"Bạn được mời tham dự hội thảo '{seminar.Content}' diễn ra vào lúc {seminar.StartTime:dd/MM/yyyy HH:mm}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (notifications.Count > 0)
                {
                    foreach (var notification in notifications)
                        await _notificationRepository.AddAsync(notification);

                    await _notificationRepository.SaveChangesAsync();
                }
            }

            response.Added = newParticipants.Count;

            foreach (var participant in participantsToSend)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var email = ResolveParticipantEmail(participant);

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Skipped++;
                    continue;
                }

                try
                {
                    await _emailService.SendEmailAsync(email, "[ARS] Seminar Invitation", BuildInvitationEmailBody(seminar));
                    participant.InvitationSentAt = DateTime.UtcNow;
                    response.Sent++;
                }
                catch (Exception ex)
                {
                    response.FailedEmails.Add(email);
                    _logger.LogWarning(ex, "Failed to send seminar {SeminarId} invitation to {Email}.", seminarId, email);
                }
            }

            if (participantsToSend.Count > 0)
                await _participantRepository.SaveChangesAsync();

            return response;
        }

        public async Task<SeminarStatsResponse?> GetStatsAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
                return null;

            var participants = seminar.SeminarParticipants.ToList();

            var declined = participants.Count(p =>
                NormalizeParticipantStatus(p.InvitationStatus) == "DECLINED");

            var submitted = participants.Count(p =>
                NormalizeParticipantStatus(p.InvitationStatus) != "DECLINED"
                && !string.IsNullOrWhiteSpace(p.FeedbackJson));

            var pending = participants.Count - submitted - declined;

            return new SeminarStatsResponse
            {
                SeminarId = seminarId,
                TotalInvited = participants.Count,
                Submitted = submitted,
                Pending = pending,
                Declined = declined,
                CompletionPercentage = participants.Count == 0
                    ? 0
                    : Math.Round((decimal)submitted / participants.Count * 100, 2)
            };
        }

        public async Task<SeminarReminderResponse> SendFeedbackRemindersAsync(int seminarId, int organizerId, CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
                throw new KeyNotFoundException("Seminar not found.");

            var eligibleParticipants = seminar.SeminarParticipants
                .Where(p =>
                {
                    var status = NormalizeParticipantStatus(p.InvitationStatus);

                    return status != "DECLINED"
                        && string.IsNullOrWhiteSpace(p.FeedbackJson)
                        && p.FeedbackReminderSentAt == null;
                })
                .ToList();

            var response = new SeminarReminderResponse
            {
                SeminarId = seminarId,
                Eligible = eligibleParticipants.Count,
                Skipped = seminar.SeminarParticipants.Count - eligibleParticipants.Count
            };

            foreach (var participant in eligibleParticipants)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var email = ResolveParticipantEmail(participant);

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Skipped++;
                    continue;
                }

                try
                {
                    await _emailService.SendEmailAsync(email, "[ARS] Seminar Feedback Reminder", BuildFeedbackReminderEmailBody(seminar));
                    participant.FeedbackReminderSentAt = DateTime.UtcNow;
                    response.Sent++;
                }
                catch (Exception ex)
                {
                    response.FailedEmails.Add(email);
                    _logger.LogWarning(ex, "Failed to send feedback reminder for seminar {SeminarId} to {Email}.", seminarId, email);
                }
            }

            if (eligibleParticipants.Count > 0)
                await _participantRepository.SaveChangesAsync();

            return response;
        }

        public async Task<bool> IsOwnedByOrganizerAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            return seminar != null && seminar.OrganizerId == organizerId;
        }

        public async Task UpdateLifecycleStatusesAsync(CancellationToken cancellationToken = default)
        {
            var seminars = await _seminarRepository.GetLifecycleCandidatesAsync();
            var now = DateTime.UtcNow;
            var hasChanges = false;

            foreach (var seminar in seminars)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip Inactive, Suspended, or Draft seminars
                if (IsInactiveOrSuspended(seminar.Status) || IsDraft(seminar.Status))
                    continue;

                var expectedStatus = CalculateLifecycleStatus(seminar.StartTime, seminar.EndTime, now);

                if (string.Equals(seminar.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
                    continue;

                seminar.Status = expectedStatus;
                _seminarRepository.Update(seminar);
                hasChanges = true;
            }

            if (hasChanges)
                await _seminarRepository.SaveChangesAsync();
        }

        public async Task SendDueEventRemindersAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var seminars = await _seminarRepository.GetDueReminderSeminarsAsync(now, now.Add(EventReminderWindow));
            var hasChanges = false;

            foreach (var seminar in seminars)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reminderCandidates = seminar.SeminarParticipants
                    .Where(p => NormalizeParticipantStatus(p.InvitationStatus) != "DECLINED")
                    .ToList();

                var eligibleParticipants = reminderCandidates
                    .Where(p => p.InvitationSentAt != null)
                    .ToList();

                foreach (var participant in eligibleParticipants.Where(p => p.EventReminderSentAt == null))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var email = ResolveParticipantEmail(participant);

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    try
                    {
                        await _emailService.SendEmailAsync(email, "[ARS] Upcoming Seminar Reminder", BuildEventReminderEmailBody(seminar));
                        participant.EventReminderSentAt = DateTime.UtcNow;
                        hasChanges = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send event reminder for seminar {SeminarId} to {Email}.", seminar.SeminarId, email);
                    }
                }

                if (reminderCandidates.Count == 0
                    || (eligibleParticipants.Count == reminderCandidates.Count
                        && eligibleParticipants.All(p => p.EventReminderSentAt != null)))
                {
                    seminar.IsReminderSent = true;
                    seminar.ReminderSentAt ??= DateTime.UtcNow;
                    _seminarRepository.Update(seminar);
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _seminarRepository.SaveChangesAsync();
        }

        public async Task<List<SuggestedInviteeDto>> GetSuggestedInviteesAsync(int subFieldId, int currentUserId)
        {
            var users = await _userRepository.GetQueryable()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.ProfessionalProfile)
                    .ThenInclude(p => p.SubField)
                .Where(u => u.UserId != currentUserId
                         && (u.IsActive == null || u.IsActive == true)
                         && u.ProfessionalProfile != null
                         && u.ProfessionalProfile.SubFieldId == subFieldId
                         && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            return users.Select(u =>
            {
                var userRoles = u.UserRoles
                    .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
                    .Select(ur => ur.Role!.Name)
                    .Distinct()
                    .ToList();

                return new SuggestedInviteeDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    Role = userRoles.Count > 0 ? string.Join(" • ", userRoles) : "Researcher",
                    Roles = userRoles,
                    SubFieldId = u.ProfessionalProfile?.SubFieldId,
                    SubFieldName = u.ProfessionalProfile?.SubField?.Name,
                    OrcidId = u.ProfessionalProfile?.OrcidId,
                    Hindex = u.ProfessionalProfile?.Hindex,
                    PublicationCount = u.ProfessionalProfile?.PublicationCount
                };
            }).ToList();
        }

        public async Task<SeminarFeedbackAiSummaryResponse> SummarizeFeedbackAsync(int seminarId, int organizerId, CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
                throw new KeyNotFoundException("Seminar not found.");

            var feedbackJsons = seminar.SeminarParticipants
                .Where(p =>
                    NormalizeParticipantStatus(p.InvitationStatus) != "DECLINED"
                    && !string.IsNullOrWhiteSpace(p.FeedbackJson))
                .Select(p => p.FeedbackJson!)
                .ToList();

            if (feedbackJsons.Count == 0)
                throw new InvalidOperationException("Seminar chưa có feedback để tổng hợp.");

            var aiFeedback = await _seminarFeedbackAiService.SummarizeFeedbackAsync(
                seminar.Content,
                seminar.Feedback,
                feedbackJsons,
                cancellationToken);

            var generatedAt = DateTime.UtcNow;

            seminar.FeedbackJson = JsonSerializer.Serialize(aiFeedback);
            seminar.AiFeedbackGeneratedAt = generatedAt;

            _seminarRepository.Update(seminar);
            await _seminarRepository.SaveChangesAsync();

            return new SeminarFeedbackAiSummaryResponse
            {
                SeminarId = seminar.SeminarId,
                FeedbackCount = feedbackJsons.Count,
                Feedback = aiFeedback,
                GeneratedAt = generatedAt
            };
        }

        private static void ValidateSeminarValues(DateTime startTime, DateTime endTime, string? content, int? maxParticipants, int existingParticipantCount)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Seminar content is required.");

            if (endTime <= startTime)
                throw new ArgumentException("EndTime must be later than StartTime.");

            if (maxParticipants.HasValue && maxParticipants.Value < 0)
                throw new ArgumentException("MaxParticipants cannot be negative.");

            if (maxParticipants.HasValue
                && maxParticipants.Value > 0
                && maxParticipants.Value < existingParticipantCount)
            {
                throw new ArgumentException("MaxParticipants cannot be lower than the current participant count.");
            }
        }

        private static List<string> NormalizeEmails(IEnumerable<string>? emails)
        {
            if (emails == null)
                return new List<string>();

            var validator = new EmailAddressAttribute();
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawEmail in emails)
            {
                var email = rawEmail?.Trim();

                if (string.IsNullOrWhiteSpace(email))
                    continue;

                if (!validator.IsValid(email))
                    throw new ArgumentException($"Invalid seminar invitation email: {email}");

                if (seen.Add(email))
                    normalized.Add(email);
            }

            return normalized;
        }

        private static bool IsDraft(string? status)
        {
            return string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInactiveOrSuspended(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase);
        }

        private static string CalculateLifecycleStatus(DateTime startTime, DateTime endTime, DateTime nowUtc)
        {
            if (endTime <= nowUtc)
                return "Completed";

            if (startTime <= nowUtc)
                return "In Progress";

            return "Upcoming";
        }

        private static string NormalizeParticipantStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "PENDING";

            var value = status.Trim().ToLowerInvariant();

            if (value is "submitted" or "complete" or "completed")
                return "SUBMITTED";

            if (value is "declined" or "rejected")
                return "DECLINED";

            if (value is "invited" or "accepted" or "confirmed")
                return "INVITED";

            return "PENDING";
        }

        private static string? ResolveParticipantEmail(SeminarParticipant participant)
        {
            if (!string.IsNullOrWhiteSpace(participant.User?.Email))
                return participant.User.Email;

            return participant.InvitedEmail;
        }

        private static string BuildInvitationEmailBody(Seminar seminar)
        {
            var content = WebUtility.HtmlEncode(seminar.Content);
            var link = WebUtility.HtmlEncode(seminar.OnlineLink ?? string.Empty);
            var safeTitle = HtmlEncoder.Default.Encode(seminar.Content ?? "Hội thảo khoa học");
            var startTime = seminar.StartTime.ToString("dd/MM/yyyy HH:mm");
            var endTime = seminar.EndTime.ToString("dd/MM/yyyy HH:mm");
            var joinUrl = !string.IsNullOrWhiteSpace(seminar.OnlineLink) ? seminar.OnlineLink : "#";
            var safeJoinUrl = HtmlEncoder.Default.Encode(joinUrl);

            return $@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]>
  <style type=""text/css"">
    body, table, td, p, a, h1, h2, h3 {{ font-family: Arial, Helvetica, sans-serif !important; }}
  </style>
  <![endif]-->
  <style type=""text/css"">
    @media screen and (max-width: 620px) {{
      .email-shell {{ width: 100% !important; }}
      .email-gutter {{ padding-left: 24px !important; padding-right: 24px !important; }}
      .info-box {{ padding-left: 20px !important; padding-right: 20px !important; }}
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f5f1e8; color:#1d1c19; font-family:Arial, Helvetica, sans-serif;""><span style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">You are invited to an ARS academic seminar.</span>
  <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f5f1e8;""><tr><td align=""center"" style=""padding:36px 16px;"">
    <table role=""presentation"" class=""email-shell"" width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border:1px solid #ded9cf;""><tr><td>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1d1c19;""><tr><td class=""email-gutter"" style=""padding:26px 40px 24px;"">
        <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:25px; line-height:30px; font-weight:bold; color:#fffdf8;"">ARS<span style=""color:#e2ad2f;"">.</span></div>
        <div style=""padding-top:7px; font-size:10px; line-height:14px; letter-spacing:2px; color:#d7d2c8;"">ACADEMIC RESEARCH SHARING</div>
      </td></tr></table>
      <div style=""height:4px; line-height:4px; font-size:4px; background-color:#e2ad2f;"">&nbsp;</div>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0""><tr><td class=""email-gutter"" style=""padding:40px;"">
        <div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Seminar invitation</div>
        <h1 style=""margin:9px 0 18px; font-family:Georgia, 'Times New Roman', serif; font-size:28px; line-height:34px; font-weight:bold; color:#1d1c19;"">Thư mời tham dự hội thảo</h1>
        <p style=""margin:0 0 18px; font-size:16px; line-height:26px; color:#4f4b42;"">Bạn nhận được lời mời tham dự hội thảo với chủ đề:</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:6px 0 22px;""><tr><td class=""info-box"" style=""padding:22px 28px; background-color:#fff9e8; border:1px solid #e2ad2f;""><div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Chủ đề hội thảo</div><div style=""padding-top:8px; font-family:Georgia, 'Times New Roman', serif; font-size:20px; line-height:28px; font-weight:bold; color:#1d1c19;"">{content}</div></td></tr></table>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:0 0 18px;""><tr><td style=""padding:14px 18px; background-color:#fffdf8; border:1px solid #ded9cf;""><p style=""margin:0 0 6px; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Thời gian bắt đầu:</strong> <span style=""margin-left:6px;"">{startTime}</span></p><p style=""margin:0 0 6px; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Thời gian kết thúc:</strong> <span style=""margin-left:6px;"">{endTime}</span></p><p style=""margin:0; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Link tham dự:</strong> <a href=""{safeJoinUrl}"" style=""margin-left:6px; color:#1d1c19; word-break:break-all;"">{link}</a></p></td></tr></table>
        <p style=""margin:0; text-align:center; font-size:15px; line-height:24px; color:#4f4b42;"">Rất mong sự góp mặt của bạn!</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-top:30px;""><tr><td style=""padding:16px; background-color:#f6f8f5; border:1px solid #d7ded7;""><p style=""margin:0 0 6px; font-size:13px; line-height:19px; font-weight:bold; color:#4f765d;"">Lưu ý</p><p style=""margin:0; font-size:13px; line-height:20px; color:#4f4b42;"">Vui lòng đăng nhập vào hệ thống ARS để xem chi tiết và xác nhận tham dự.</p></td></tr></table>
      </td></tr></table>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top:1px solid #ded9cf; background-color:#fffdf8;""><tr><td class=""email-gutter"" align=""center"" style=""padding:20px 40px;""><p style=""margin:0; font-size:12px; line-height:19px; color:#6f695d;"">Academic Research Sharing Platform</p><p style=""margin:5px 0 0; font-size:12px; line-height:19px; color:#6f695d;"">This message was sent automatically. Please do not reply.</p></td></tr></table>
    </td></tr></table>
  </td></tr></table>
</body>
</html>";
        }

        private static string BuildEventReminderEmailBody(Seminar seminar)
        {
            var content = WebUtility.HtmlEncode(seminar.Content);
            var link = WebUtility.HtmlEncode(seminar.OnlineLink ?? string.Empty);
            var startTime = seminar.StartTime.ToString("dd/MM/yyyy HH:mm");
            var joinUrl = !string.IsNullOrWhiteSpace(seminar.OnlineLink) ? seminar.OnlineLink : "#";
            var safeJoinUrl = HtmlEncoder.Default.Encode(joinUrl);

            return $@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]>
  <style type=""text/css"">
    body, table, td, p, a, h1, h2, h3 {{ font-family: Arial, Helvetica, sans-serif !important; }}
  </style>
  <![endif]-->
  <style type=""text/css"">
    @media screen and (max-width: 620px) {{
      .email-shell {{ width: 100% !important; }}
      .email-gutter {{ padding-left: 24px !important; padding-right: 24px !important; }}
      .info-box {{ padding-left: 20px !important; padding-right: 20px !important; }}
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f5f1e8; color:#1d1c19; font-family:Arial, Helvetica, sans-serif;""><span style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">Reminder: an ARS academic seminar is starting soon.</span>
  <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f5f1e8;""><tr><td align=""center"" style=""padding:36px 16px;"">
    <table role=""presentation"" class=""email-shell"" width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border:1px solid #ded9cf;""><tr><td>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1d1c19;""><tr><td class=""email-gutter"" style=""padding:26px 40px 24px;"">
        <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:25px; line-height:30px; font-weight:bold; color:#fffdf8;"">ARS<span style=""color:#e2ad2f;"">.</span></div>
        <div style=""padding-top:7px; font-size:10px; line-height:14px; letter-spacing:2px; color:#d7d2c8;"">ACADEMIC RESEARCH SHARING</div>
      </td></tr></table>
      <div style=""height:4px; line-height:4px; font-size:4px; background-color:#e2ad2f;"">&nbsp;</div>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0""><tr><td class=""email-gutter"" style=""padding:40px;"">
        <div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Seminar reminder</div>
        <h1 style=""margin:9px 0 18px; font-family:Georgia, 'Times New Roman', serif; font-size:28px; line-height:34px; font-weight:bold; color:#1d1c19;"">Nhắc nhở: Hội thảo sắp diễn ra</h1>
        <p style=""margin:0 0 18px; font-size:16px; line-height:26px; color:#4f4b42;"">Hội thảo dưới đây sắp sửa diễn ra. Vui lòng sắp xếp thời gian để tham dự đúng giờ.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:6px 0 22px;""><tr><td class=""info-box"" style=""padding:22px 28px; background-color:#fff9e8; border:1px solid #e2ad2f;""><div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Chủ đề hội thảo</div><div style=""padding-top:8px; font-family:Georgia, 'Times New Roman', serif; font-size:20px; line-height:28px; font-weight:bold; color:#1d1c19;"">{content}</div></td></tr></table>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:0 0 18px;""><tr><td style=""padding:14px 18px; background-color:#fffdf8; border:1px solid #ded9cf;""><p style=""margin:0 0 6px; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Thời gian:</strong> <span style=""margin-left:6px;"">{startTime}</span></p><p style=""margin:0; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Link tham dự:</strong> <a href=""{safeJoinUrl}"" style=""margin-left:6px; color:#1d1c19; word-break:break-all;"">{link}</a></p></td></tr></table>
        <p style=""margin:0; text-align:center; font-size:15px; line-height:24px; color:#4f4b42;"">Đừng bỏ lỡ buổi hội thảo này nhé!</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-top:30px;""><tr><td style=""padding:16px; background-color:#f6f8f5; border:1px solid #d7ded7;""><p style=""margin:0 0 6px; font-size:13px; line-height:19px; font-weight:bold; color:#4f765d;"">Mẹo nhỏ</p><p style=""margin:0; font-size:13px; line-height:20px; color:#4f4b42;"">Hãy đăng nhập ARS trước giờ bắt đầu khoảng 5 phút để kiểm tra âm thanh và đường truyền.</p></td></tr></table>
      </td></tr></table>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top:1px solid #ded9cf; background-color:#fffdf8;""><tr><td class=""email-gutter"" align=""center"" style=""padding:20px 40px;""><p style=""margin:0; font-size:12px; line-height:19px; color:#6f695d;"">Academic Research Sharing Platform</p><p style=""margin:5px 0 0; font-size:12px; line-height:19px; color:#6f695d;"">This message was sent automatically. Please do not reply.</p></td></tr></table>
    </td></tr></table>
  </td></tr></table>
</body>
</html>";
        }

        private static string BuildFeedbackReminderEmailBody(Seminar seminar)
        {
            var content = WebUtility.HtmlEncode(seminar.Content);
            var endTime = seminar.EndTime.ToString("dd/MM/yyyy HH:mm");

            return $@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]>
  <style type=""text/css"">
    body, table, td, p, a, h1, h2, h3 {{ font-family: Arial, Helvetica, sans-serif !important; }}
  </style>
  <![endif]-->
  <style type=""text/css"">
    @media screen and (max-width: 620px) {{
      .email-shell {{ width: 100% !important; }}
      .email-gutter {{ padding-left: 24px !important; padding-right: 24px !important; }}
      .info-box {{ padding-left: 20px !important; padding-right: 20px !important; }}
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f5f1e8; color:#1d1c19; font-family:Arial, Helvetica, sans-serif;""><span style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">Please share your feedback for the ARS seminar you attended.</span>
  <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f5f1e8;""><tr><td align=""center"" style=""padding:36px 16px;"">
    <table role=""presentation"" class=""email-shell"" width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border:1px solid #ded9cf;""><tr><td>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1d1c19;""><tr><td class=""email-gutter"" style=""padding:26px 40px 24px;"">
        <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:25px; line-height:30px; font-weight:bold; color:#fffdf8;"">ARS<span style=""color:#e2ad2f;"">.</span></div>
        <div style=""padding-top:7px; font-size:10px; line-height:14px; letter-spacing:2px; color:#d7d2c8;"">ACADEMIC RESEARCH SHARING</div>
      </td></tr></table>
      <div style=""height:4px; line-height:4px; font-size:4px; background-color:#e2ad2f;"">&nbsp;</div>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0""><tr><td class=""email-gutter"" style=""padding:40px;"">
        <div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Feedback reminder</div>
        <h1 style=""margin:9px 0 18px; font-family:Georgia, 'Times New Roman', serif; font-size:28px; line-height:34px; font-weight:bold; color:#1d1c19;"">Khảo sát hội thảo</h1>
        <p style=""margin:0 0 18px; font-size:16px; line-height:26px; color:#4f4b42;"">Cảm ơn bạn đã tham dự hội thảo. Bạn hiện còn feedback chưa gửi cho buổi hội thảo dưới đây.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:6px 0 22px;""><tr><td class=""info-box"" style=""padding:22px 28px; background-color:#fff9e8; border:1px solid #e2ad2f;""><div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Chủ đề hội thảo</div><div style=""padding-top:8px; font-family:Georgia, 'Times New Roman', serif; font-size:20px; line-height:28px; font-weight:bold; color:#1d1c19;"">{content}</div></td></tr></table>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:0 0 18px;""><tr><td style=""padding:14px 18px; background-color:#fffdf8; border:1px solid #ded9cf;""><p style=""margin:0; font-size:14px; line-height:20px; color:#1d1c19;""><strong style=""color:#6f695d; letter-spacing:1px; text-transform:uppercase; font-size:11px;"">Thời gian kết thúc:</strong> <span style=""margin-left:6px;"">{endTime}</span></p></td></tr></table>
        <p style=""margin:0; text-align:center; font-size:15px; line-height:24px; color:#4f4b42;"">Vui lòng đăng nhập vào hệ thống để gửi feedback của bạn.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-top:30px;""><tr><td style=""padding:16px; background-color:#f6f8f5; border:1px solid #d7ded7;""><p style=""margin:0 0 6px; font-size:13px; line-height:19px; font-weight:bold; color:#4f765d;"">Vì sao cần feedback?</p><p style=""margin:0; font-size:13px; line-height:20px; color:#4f4b42;"">Những đánh giá, góp ý của bạn sẽ giúp các chương trình tiếp theo diễn ra tốt đẹp hơn.</p></td></tr></table>
      </td></tr></table>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top:1px solid #ded9cf; background-color:#fffdf8;""><tr><td class=""email-gutter"" align=""center"" style=""padding:20px 40px;""><p style=""margin:0; font-size:12px; line-height:19px; color:#6f695d;"">Academic Research Sharing Platform</p><p style=""margin:5px 0 0; font-size:12px; line-height:19px; color:#6f695d;"">This message was sent automatically. Please do not reply.</p></td></tr></table>
    </td></tr></table>
  </td></tr></table>
</body>
</html>";
        }

        public async Task<SeminarFeedbackFormResponse> UpdateFeedbackFormAsync(int seminarId, int organizerId, object rawPayload, bool isAdmin = false)
        {
            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            if (seminar == null)
                throw new KeyNotFoundException($"Seminar with ID {seminarId} not found.");

            if (!isAdmin && seminar.OrganizerId != organizerId)
                throw new UnauthorizedAccessException("You are not authorized to configure feedback form for this seminar.");

            var (jsonString, questions) = ParseAndNormalizeQuestions(rawPayload);

            seminar.Feedback = jsonString;
            _seminarRepository.Update(seminar);
            await _seminarRepository.SaveChangesAsync();

            return new SeminarFeedbackFormResponse
            {
                SeminarId = seminarId,
                Feedback = jsonString,
                Questions = questions,
                Message = "Feedback form updated successfully."
            };
        }

        public async Task<SeminarFeedbackFormResponse?> GetFeedbackFormAsync(int seminarId, int currentUserId, bool isAdmin = false)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);
            if (seminar == null)
                return null;

            if (!isAdmin && seminar.OrganizerId != currentUserId)
            {
                var currentUser = await _userRepository.GetByIdAsync(currentUserId);
                var currentUserEmail = currentUser?.Email;

                var isParticipant = seminar.SeminarParticipants.Any(participant =>
                    NormalizeParticipantStatus(participant.InvitationStatus) != "DECLINED"
                    && (participant.UserId == currentUserId
                        || (!string.IsNullOrWhiteSpace(currentUserEmail)
                            && string.Equals(participant.InvitedEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))));

                if (!isParticipant)
                    return null;
            }

            var questions = new List<SeminarFeedbackQuestionDto>();
            if (!string.IsNullOrWhiteSpace(seminar.Feedback))
            {
                try
                {
                    questions = JsonSerializer.Deserialize<List<SeminarFeedbackQuestionDto>>(seminar.Feedback, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SeminarFeedbackQuestionDto>();
                }
                catch
                {
                    // Trả về danh sách rỗng nếu feedback không phải danh sách câu hỏi
                }
            }

            return new SeminarFeedbackFormResponse
            {
                SeminarId = seminarId,
                Feedback = seminar.Feedback,
                Questions = questions,
                Message = "Feedback form retrieved successfully."
            };
        }
        private static (string jsonString, List<SeminarFeedbackQuestionDto> questions) ParseAndNormalizeQuestions(object rawPayload)
        {
            if (rawPayload == null)
                throw new ArgumentException("Feedback questions payload is required.");

            string? candidateJson = null;
            List<SeminarFeedbackQuestionDto>? candidateList = null;

            if (rawPayload is string str)
            {
                candidateJson = str.Trim();
            }
            else if (rawPayload is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    candidateJson = jsonElement.GetString()?.Trim();
                }
                else if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    candidateJson = jsonElement.GetRawText();
                }
                else if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    if (jsonElement.TryGetProperty("feedback", out var fbProp))
                    {
                        if (fbProp.ValueKind == JsonValueKind.String)
                            candidateJson = fbProp.GetString()?.Trim();
                        else if (fbProp.ValueKind == JsonValueKind.Array)
                            candidateJson = fbProp.GetRawText();
                    }
                    else if (jsonElement.TryGetProperty("questions", out var qProp) && qProp.ValueKind == JsonValueKind.Array)
                    {
                        candidateJson = qProp.GetRawText();
                    }
                }
            }
            else if (rawPayload is SeminarFeedbackFormRequest formRequest)
            {
                if (formRequest.Questions != null && formRequest.Questions.Count > 0)
                {
                    candidateList = formRequest.Questions;
                }
                else if (formRequest.Feedback is string fbStr)
                {
                    candidateJson = fbStr.Trim();
                }
                else if (formRequest.Feedback is JsonElement fbEl)
                {
                    if (fbEl.ValueKind == JsonValueKind.String)
                        candidateJson = fbEl.GetString()?.Trim();
                    else
                        candidateJson = fbEl.GetRawText();
                }
                else if (formRequest.Feedback != null)
                {
                    candidateJson = JsonSerializer.Serialize(formRequest.Feedback);
                }
            }
            else if (rawPayload is IEnumerable<SeminarFeedbackQuestionDto> list)
            {
                candidateList = list.ToList();
            }
            else
            {
                candidateJson = JsonSerializer.Serialize(rawPayload);
            }

            if (candidateList == null)
            {
                if (string.IsNullOrWhiteSpace(candidateJson))
                    throw new ArgumentException("Feedback questions payload cannot be empty.");

                try
                {
                    using var doc = JsonDocument.Parse(candidateJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("feedback", out var fb))
                        {
                            if (fb.ValueKind == JsonValueKind.String)
                                candidateJson = fb.GetString()?.Trim();
                            else if (fb.ValueKind == JsonValueKind.Array)
                                candidateJson = fb.GetRawText();
                        }
                        else if (doc.RootElement.TryGetProperty("questions", out var qs) && qs.ValueKind == JsonValueKind.Array)
                        {
                            candidateJson = qs.GetRawText();
                        }
                    }
                }
                catch
                {
                    // Tiếp tục thử parse dạng danh sách câu hỏi
                }

                try
                {
                    candidateList = JsonSerializer.Deserialize<List<SeminarFeedbackQuestionDto>>(candidateJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"Invalid JSON format for feedback questions: {ex.Message}");
                }
            }

            if (candidateList == null || candidateList.Count == 0)
                throw new ArgumentException("At least one feedback question is required.");

            var normalizedQuestions = new List<SeminarFeedbackQuestionDto>();
            for (int i = 0; i < candidateList.Count; i++)
            {
                var q = candidateList[i];
                var type = string.Equals(q.Type, "rating", StringComparison.OrdinalIgnoreCase) ? "rating" : "text";
                var id = !string.IsNullOrWhiteSpace(q.Id) ? q.Id.Trim() : $"q_{i + 1}";
                var text = q.QuestionText?.Trim() ?? string.Empty;
                var maxStar = type == "rating" ? (q.MaxStar.HasValue && q.MaxStar.Value > 0 ? q.MaxStar.Value : 5) : (int?)null;

                normalizedQuestions.Add(new SeminarFeedbackQuestionDto
                {
                    Id = id,
                    OrderIndex = q.OrderIndex >= 0 ? q.OrderIndex : i,
                    Type = type,
                    QuestionText = text,
                    IsRequired = q.IsRequired,
                    MaxStar = maxStar
                });
            }

            var finalJson = JsonSerializer.Serialize(normalizedQuestions, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return (finalJson, normalizedQuestions);
        }
    }
}