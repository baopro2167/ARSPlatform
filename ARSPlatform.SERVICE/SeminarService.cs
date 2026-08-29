using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class SeminarService : ISeminarService
    {
        private static readonly TimeSpan EventReminderWindow = TimeSpan.FromHours(24);

        private readonly ISeminarRepository _seminarRepository;
        private readonly ISeminarParticipantRepository _participantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGoogleMeetService _googleMeetService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<SeminarService> _logger;

        public SeminarService(
            ISeminarRepository seminarRepository,
            ISeminarParticipantRepository participantRepository,
            IUserRepository userRepository,
            IGoogleMeetService googleMeetService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<SeminarService> logger)
        {
            _seminarRepository = seminarRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
            _googleMeetService = googleMeetService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SeminarResponse>> GetAllAsync(int? organizerId = null)
        {
            var seminars = organizerId.HasValue
                ? await _seminarRepository.GetAllForOrganizerWithParticipantsAsync(organizerId.Value)
                : await _seminarRepository.GetAllWithParticipantsAsync();

            return _mapper.Map<IEnumerable<SeminarResponse>>(seminars);
        }

        public async Task<PagedResult<SeminarResponse>> GetPagedAsync(PaginationParams paginationParams, int? organizerId = null)
        {
            var paged = organizerId.HasValue
                ? await _seminarRepository.GetPagedAsync(
                    paginationParams,
                    predicate: x => x.OrganizerId == organizerId.Value,
                    orderBy: q => q.OrderByDescending(x => x.StartTime),
                    includes: x => x.SeminarParticipants)
                : await _seminarRepository.GetPagedAsync(
                    paginationParams,
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
            {
                return null;
            }

            if (organizerId.HasValue && seminar.OrganizerId != organizerId.Value)
            {
                return null;
            }

            return _mapper.Map<SeminarResponse>(seminar);
        }

        public async Task<SeminarResponse> CreateAsync(
            int organizerId,
            SeminarCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateSeminarValues(
                request.StartTime,
                request.EndTime,
                request.Content,
                request.MaxParticipants,
                0);

            var normalizedGuestEmails = NormalizeEmails(request.GuestEmails);

            if (request.MaxParticipants.HasValue
                && request.MaxParticipants.Value > 0
                && normalizedGuestEmails.Count > request.MaxParticipants.Value)
            {
                throw new ArgumentException(
                    "Guest email count exceeds MaxParticipants.");
            }

            var googleMeetLink = await _googleMeetService
                .CreateMeetingSpaceAsync(cancellationToken);

            var seminar = _mapper.Map<Seminar>(request);

            seminar.OrganizerId = organizerId;
            seminar.OnlineLink = googleMeetLink;
            seminar.ReminderEnabled = request.IsReminderSent ?? false;
            seminar.IsReminderSent = false;
            seminar.ReminderSentAt = null;
            seminar.Status = IsDraft(request.Status)
                ? "Draft"
                : CalculateLifecycleStatus(
                    request.StartTime,
                    request.EndTime,
                    DateTime.UtcNow);

            await _seminarRepository.AddAsync(seminar);

            var participants = new List<SeminarParticipant>();

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
            }

            // Seminar and initial participant rows share the same scoped AppDbContext.
            // One SaveChanges persists the database registration state together.
            await _seminarRepository.SaveChangesAsync();

            if (participants.Count > 0)
            {
                var invitationTimestampChanged = false;

                foreach (var participant in participants)
                {
                    var email = ResolveParticipantEmail(participant);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    try
                    {
                        await _emailService.SendEmailAsync(
                            email,
                            "[ARS] Seminar Invitation",
                            BuildInvitationEmailBody(seminar));

                        participant.InvitationSentAt = DateTime.UtcNow;
                        invitationTimestampChanged = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send seminar {SeminarId} invitation to {Email}.",
                            seminar.SeminarId,
                            email);
                    }
                }

                if (invitationTimestampChanged)
                {
                    await _participantRepository.SaveChangesAsync();
                }
            }

            var created = await _seminarRepository
                .GetByIdWithParticipantsAsync(seminar.SeminarId);

            return _mapper.Map<SeminarResponse>(created ?? seminar);
        }

        public async Task<SeminarResponse?> UpdateAsync(
            int seminarId,
            int organizerId,
            SeminarUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                return null;
            }

            var startTime = request.StartTime ?? seminar.StartTime;
            var endTime = request.EndTime ?? seminar.EndTime;
            var content = request.Content ?? seminar.Content;
            var maxParticipants = request.MaxParticipants ?? seminar.MaxParticipants;

            ValidateSeminarValues(
                startTime,
                endTime,
                content,
                maxParticipants,
                seminar.SeminarParticipants.Count);

            if (request.StartTime.HasValue)
            {
                seminar.StartTime = request.StartTime.Value;
                seminar.IsReminderSent = false;
                seminar.ReminderSentAt = null;

                foreach (var participant in seminar.SeminarParticipants)
                {
                    participant.EventReminderSentAt = null;
                }
            }

            if (request.EndTime.HasValue)
            {
                seminar.EndTime = request.EndTime.Value;
            }

            if (request.Content != null)
            {
                seminar.Content = request.Content.Trim();
            }

            if (request.MaxParticipants.HasValue)
            {
                seminar.MaxParticipants = request.MaxParticipants.Value;
            }

            if (request.ReminderEnabled.HasValue)
            {
                seminar.ReminderEnabled = request.ReminderEnabled.Value;

                if (!request.ReminderEnabled.Value)
                {
                    seminar.IsReminderSent = false;
                    seminar.ReminderSentAt = null;
                }
            }

            if (IsDraft(request.Status))
            {
                seminar.Status = "Draft";
            }
            else if (!IsDraft(seminar.Status)
                || !string.IsNullOrWhiteSpace(request.Status))
            {
                seminar.Status = CalculateLifecycleStatus(
                    seminar.StartTime,
                    seminar.EndTime,
                    DateTime.UtcNow);
            }

            _seminarRepository.Update(seminar);
            await _seminarRepository.SaveChangesAsync();

            // FE30 currently uses PUT { isReminderSent: true } for "Remind Pending".
            // Preserve that contract while the canonical endpoint also exists.
            if (request.IsReminderSent == true)
            {
                await SendFeedbackRemindersAsync(
                    seminarId,
                    organizerId,
                    cancellationToken);
            }

            var updated = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);
            return _mapper.Map<SeminarResponse>(updated ?? seminar);
        }

        public async Task<bool> DeleteAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                return false;
            }

            _seminarRepository.Delete(seminar);
            await _seminarRepository.SaveChangesAsync();
            return true;
        }

        public async Task<SeminarInviteResponse> InviteAsync(
            int seminarId,
            int organizerId,
            SeminarInviteRequest request,
            CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                throw new KeyNotFoundException("Seminar not found.");
            }

            var normalizedEmails = NormalizeEmails(request.Emails);

            if (normalizedEmails.Count == 0)
            {
                throw new ArgumentException("At least one valid email is required.");
            }

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
                    || string.Equals(
                        p.InvitedEmail,
                        email,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        p.User?.Email,
                        email,
                        StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (existing.InvitationSentAt == null)
                    {
                        participantsToSend.Add(existing);
                    }
                    else
                    {
                        response.Skipped++;
                    }

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
                && existingParticipants.Count + newParticipants.Count
                    > seminar.MaxParticipants.Value)
            {
                throw new InvalidOperationException(
                    "Invitations would exceed the seminar MaxParticipants limit.");
            }

            foreach (var participant in newParticipants)
            {
                await _participantRepository.AddAsync(participant);
            }

            if (newParticipants.Count > 0)
            {
                await _participantRepository.SaveChangesAsync();
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
                    await _emailService.SendEmailAsync(
                        email,
                        "[ARS] Seminar Invitation",
                        BuildInvitationEmailBody(seminar));

                    participant.InvitationSentAt = DateTime.UtcNow;
                    response.Sent++;
                }
                catch (Exception ex)
                {
                    response.FailedEmails.Add(email);

                    _logger.LogWarning(
                        ex,
                        "Failed to send seminar {SeminarId} invitation to {Email}.",
                        seminarId,
                        email);
                }
            }

            if (participantsToSend.Count > 0)
            {
                await _participantRepository.SaveChangesAsync();
            }

            return response;
        }

        public async Task<SeminarStatsResponse?> GetStatsAsync(
            int seminarId,
            int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                return null;
            }

            var participants = seminar.SeminarParticipants.ToList();

            var declined = participants.Count(p =>
                NormalizeParticipantStatus(p.InvitationStatus) == "DECLINED");

            var submitted = participants.Count(p =>
            {
                var status = NormalizeParticipantStatus(p.InvitationStatus);

                return status != "DECLINED"
                    && (status == "SUBMITTED"
                        || !string.IsNullOrWhiteSpace(p.ParticipantEvaluation));
            });

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
                    : Math.Round(
                        (decimal)submitted / participants.Count * 100,
                        2),

                // DB currently has textual ParticipantEvaluation only.
                // Do not invent a numerical score.
                AverageScore = null
            };
        }

        public async Task<SeminarReminderResponse> SendFeedbackRemindersAsync(
            int seminarId,
            int organizerId,
            CancellationToken cancellationToken = default)
        {
            var seminar = await _seminarRepository.GetByIdWithParticipantsAsync(seminarId);

            if (seminar == null || seminar.OrganizerId != organizerId)
            {
                throw new KeyNotFoundException("Seminar not found.");
            }

            var eligibleParticipants = seminar.SeminarParticipants
                .Where(p =>
                {
                    var status = NormalizeParticipantStatus(p.InvitationStatus);

                    return status != "SUBMITTED"
                        && status != "DECLINED"
                        && string.IsNullOrWhiteSpace(p.ParticipantEvaluation)
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
                    await _emailService.SendEmailAsync(
                        email,
                        "[ARS] Seminar Feedback Reminder",
                        BuildFeedbackReminderEmailBody(seminar));

                    participant.FeedbackReminderSentAt = DateTime.UtcNow;
                    response.Sent++;
                }
                catch (Exception ex)
                {
                    response.FailedEmails.Add(email);

                    _logger.LogWarning(
                        ex,
                        "Failed to send feedback reminder for seminar {SeminarId} to {Email}.",
                        seminarId,
                        email);
                }
            }

            if (eligibleParticipants.Count > 0)
            {
                await _participantRepository.SaveChangesAsync();
            }

            return response;
        }

        public async Task<bool> IsOwnedByOrganizerAsync(int seminarId, int organizerId)
        {
            var seminar = await _seminarRepository.GetByIdAsync(seminarId);

            return seminar != null
                && seminar.OrganizerId == organizerId;
        }

        public async Task UpdateLifecycleStatusesAsync(
            CancellationToken cancellationToken = default)
        {
            var seminars = await _seminarRepository.GetLifecycleCandidatesAsync();

            var now = DateTime.UtcNow;
            var hasChanges = false;

            foreach (var seminar in seminars)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var expectedStatus = CalculateLifecycleStatus(
                    seminar.StartTime,
                    seminar.EndTime,
                    now);

                if (string.Equals(
                    seminar.Status,
                    expectedStatus,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                seminar.Status = expectedStatus;

                _seminarRepository.Update(seminar);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _seminarRepository.SaveChangesAsync();
            }
        }

        public async Task SendDueEventRemindersAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var seminars = await _seminarRepository.GetDueReminderSeminarsAsync(
                now,
                now.Add(EventReminderWindow));

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

                foreach (var participant in eligibleParticipants
                    .Where(p => p.EventReminderSentAt == null))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var email = ResolveParticipantEmail(participant);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    try
                    {
                        await _emailService.SendEmailAsync(
                            email,
                            "[ARS] Upcoming Seminar Reminder",
                            BuildEventReminderEmailBody(seminar));

                        participant.EventReminderSentAt = DateTime.UtcNow;
                        hasChanges = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send event reminder for seminar {SeminarId} to {Email}.",
                            seminar.SeminarId,
                            email);
                    }
                }

                if (reminderCandidates.Count == 0
                    || (eligibleParticipants.Count == reminderCandidates.Count
                        && eligibleParticipants.All(
                            p => p.EventReminderSentAt != null)))
                {
                    seminar.IsReminderSent = true;
                    seminar.ReminderSentAt ??= DateTime.UtcNow;

                    _seminarRepository.Update(seminar);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _seminarRepository.SaveChangesAsync();
            }
        }

        private static void ValidateSeminarValues(
            DateTime startTime,
            DateTime endTime,
            string? content,
            int? maxParticipants,
            int existingParticipantCount)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "Seminar content is required.");
            }

            if (endTime <= startTime)
            {
                throw new ArgumentException(
                    "EndTime must be later than StartTime.");
            }

            if (maxParticipants.HasValue
                && maxParticipants.Value < 0)
            {
                throw new ArgumentException(
                    "MaxParticipants cannot be negative.");
            }

            if (maxParticipants.HasValue
                && maxParticipants.Value > 0
                && maxParticipants.Value < existingParticipantCount)
            {
                throw new ArgumentException(
                    "MaxParticipants cannot be lower than the current participant count.");
            }
        }

        private static List<string> NormalizeEmails(
            IEnumerable<string>? emails)
        {
            if (emails == null)
            {
                return new List<string>();
            }

            var validator = new EmailAddressAttribute();

            var normalized = new List<string>();

            var seen = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var rawEmail in emails)
            {
                var email = rawEmail?.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                if (!validator.IsValid(email))
                {
                    throw new ArgumentException(
                        $"Invalid seminar invitation email: {email}");
                }

                if (seen.Add(email))
                {
                    normalized.Add(email);
                }
            }

            return normalized;
        }

        private static bool IsDraft(string? status)
        {
            return string.Equals(
                status,
                "Draft",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string CalculateLifecycleStatus(
            DateTime startTime,
            DateTime endTime,
            DateTime nowUtc)
        {
            if (endTime <= nowUtc)
            {
                return "Completed";
            }

            if (startTime <= nowUtc)
            {
                return "In Progress";
            }

            return "Upcoming";
        }

        private static string NormalizeParticipantStatus(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "PENDING";
            }

            var value = status.Trim().ToLowerInvariant();

            if (value is "submitted" or "complete" or "completed")
            {
                return "SUBMITTED";
            }

            if (value is "declined" or "rejected")
            {
                return "DECLINED";
            }

            if (value is "invited" or "accepted" or "confirmed")
            {
                return "INVITED";
            }

            return "PENDING";
        }

        private static string? ResolveParticipantEmail(
            SeminarParticipant participant)
        {
            if (!string.IsNullOrWhiteSpace(
                participant.User?.Email))
            {
                return participant.User.Email;
            }

            return participant.InvitedEmail;
        }

        private static string BuildInvitationEmailBody(
            Seminar seminar)
        {
            var content =
                WebUtility.HtmlEncode(seminar.Content);

            var link =
                WebUtility.HtmlEncode(
                    seminar.OnlineLink ?? string.Empty);

            return $@"
<div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
  <h2>ARS Seminar Invitation</h2>
  <p>You have been invited to an academic seminar.</p>
  <p><strong>Details:</strong> {content}</p>
  <p><strong>Start:</strong> {seminar.StartTime:yyyy-MM-dd HH:mm}</p>
  <p><strong>End:</strong> {seminar.EndTime:yyyy-MM-dd HH:mm}</p>
  <p><a href=""{link}"">Join Google Meet</a></p>
</div>";
        }

        private static string BuildEventReminderEmailBody(
            Seminar seminar)
        {
            var content =
                WebUtility.HtmlEncode(seminar.Content);

            var link =
                WebUtility.HtmlEncode(
                    seminar.OnlineLink ?? string.Empty);

            return $@"
<div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
  <h2>Upcoming ARS Seminar</h2>
  <p>This is a reminder for your upcoming seminar.</p>
  <p><strong>Details:</strong> {content}</p>
  <p><strong>Start:</strong> {seminar.StartTime:yyyy-MM-dd HH:mm}</p>
  <p><a href=""{link}"">Join Google Meet</a></p>
</div>";
        }

        private static string BuildFeedbackReminderEmailBody(
            Seminar seminar)
        {
            var content =
                WebUtility.HtmlEncode(seminar.Content);

            return $@"
<div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
  <h2>ARS Seminar Feedback Reminder</h2>
  <p>Please submit your pending seminar feedback/evaluation.</p>
  <p><strong>Seminar:</strong> {content}</p>
</div>";
        }
    }
}