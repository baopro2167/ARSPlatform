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
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        public async Task<IEnumerable<SeminarParticipantResponse>?> GetFeedbackBySeminarIdAsync(int seminarId, int organizerId, bool isAdmin = false)
        {
            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            if (seminar == null || (!isAdmin && seminar.OrganizerId != organizerId))
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
            if (hasFeedbackPayload)
            {
                if (isOrganizer)
                    throw new UnauthorizedAccessException("Seminar owner cannot submit or edit feedback on behalf of a participant.");

                if (isParticipant)
                    throw new ArgumentException("Participant feedback must be submitted through POST /api/Seminar/{seminarId}/feedback.");
            }

            var oldStatus = item.InvitationStatus;
            if (request.InvitationStatus != null)
                item.InvitationStatus = NormalizeParticipantStatus(request.InvitationStatus);

            if (isParticipant && item.UserId == null)
                item.UserId = currentUserId;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Notify if participant confirmed/accepted
            if (isParticipant && item.InvitationStatus is "CONFIRMED" or "ACCEPTED" && oldStatus != item.InvitationStatus)
            {
                try
                {
                    var seminarTitle = !string.IsNullOrWhiteSpace(item.Seminar?.Content) ? item.Seminar.Content : "Hội thảo";
                    var timeStr = item.Seminar?.StartTime != null ? $" (bắt đầu lúc {item.Seminar.StartTime:HH:mm dd/MM/yyyy})" : "";
                    
                    // 1. Notify participant
                    var notifParticipant = new Notification
                    {
                        UserId = currentUserId,
                        Message = $"Bạn đã đăng ký tham gia hội thảo: \"{seminarTitle}\"{timeStr}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notifParticipant);

                    // 2. Notify organizer
                    if (item.Seminar?.OrganizerId != null && item.Seminar.OrganizerId != currentUserId)
                    {
                        var participantName = currentUser?.FullName ?? "Người tham dự";
                        var notifOrganizer = new Notification
                        {
                            UserId = item.Seminar.OrganizerId.Value,
                            Message = $"[Hội thảo] {participantName} đã xác nhận tham gia hội thảo \"{seminarTitle}\".",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationRepository.AddAsync(notifOrganizer);
                    }
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Ignore notification error
                }
            }

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
            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            if (currentUser == null)
                throw new UnauthorizedAccessException("User not found.");

            var seminar = await _seminarRepository.GetByIdAsync(seminarId);
            if (seminar == null)
                throw new KeyNotFoundException($"Seminar with ID {seminarId} not found.");

            var participant = await _repository.GetBySeminarAndUserAsync(seminarId, currentUserId, currentUser.Email);
            if (participant == null)
                throw new InvalidOperationException("You are not registered or invited to this seminar.");

            if (NormalizeParticipantStatus(participant.InvitationStatus ?? "PENDING") == "DECLINED")
                throw new InvalidOperationException("Declined participant cannot submit feedback.");

            string finalFeedbackJson;
            SeminarFeedbackContentResponse? legacyFeedback = null;
            List<SeminarFeedbackAnswerDto>? parsedAnswers = null;

            if (HasDynamicFeedbackPayload(request, out _, out parsedAnswers))
            {
                if (parsedAnswers == null || parsedAnswers.Count == 0)
                    throw new ArgumentException("Feedback answers must be a valid JSON array.");

                if (string.IsNullOrWhiteSpace(seminar.Feedback))
                    throw new InvalidOperationException("Feedback form has not been configured for this seminar.");

                parsedAnswers = ValidateAndNormalizeDynamicAnswers(seminar.Feedback, parsedAnswers);
                finalFeedbackJson = SerializeDynamicFeedback(parsedAnswers);

                var firstText = parsedAnswers.FirstOrDefault(a =>
                    string.Equals(a.Type, "text", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(a.Text))?.Text;

                legacyFeedback = new SeminarFeedbackContentResponse
                {
                    OverallComment = firstText
                };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(seminar.Feedback))
                    throw new ArgumentException("This seminar uses a dynamic feedback form. Submit feedback through the Answers or FeedbackJson field.");

                legacyFeedback = NormalizeFeedback(request.Feedback, request.ParticipantEvaluation);
                finalFeedbackJson = SerializeFeedback(legacyFeedback);
            }

            if (participant.UserId == null)
                participant.UserId = currentUserId;

            var now = DateTime.UtcNow;
            participant.FeedbackJson = finalFeedbackJson;
            participant.FeedbackSubmittedAt ??= now;
            participant.FeedbackUpdatedAt = now;
            participant.InvitationStatus = "SUBMITTED";

            _repository.Update(participant);
            await _repository.SaveChangesAsync();

            if (seminar.OrganizerId != null)
                await TryCreateFeedbackNotificationAsync(seminar.OrganizerId.Value, currentUser, seminar.Content);

            return new SeminarFeedbackResponse
            {
                SeminarId = seminarId,
                SeminarParticipantId = participant.SeminarParticipantId,
                UserId = participant.UserId,
                FeedbackJson = participant.FeedbackJson,
                Answers = parsedAnswers,
                Feedback = legacyFeedback,
                ParticipantEvaluation = legacyFeedback?.OverallComment,
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
            if (value == "invited") return "INVITED";
            if (value is "accepted" or "confirmed") return "CONFIRMED";
            if (value is "submitted" or "complete" or "completed") return "SUBMITTED";
            if (value is "declined" or "rejected") return "DECLINED";
            throw new ArgumentException("InvitationStatus must be PENDING, INVITED, CONFIRMED, SUBMITTED, or DECLINED.");
        }

        private static List<SeminarFeedbackAnswerDto> ValidateAndNormalizeDynamicAnswers(string feedbackFormJson, List<SeminarFeedbackAnswerDto> answers)
        {
            List<SeminarFeedbackQuestionDto>? questions;

            try
            {
                questions = JsonSerializer.Deserialize<List<SeminarFeedbackQuestionDto>>(feedbackFormJson, FeedbackJsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Feedback form configuration is invalid.", ex);
            }

            if (questions == null || questions.Count == 0)
                throw new InvalidOperationException("Feedback form has no questions.");

            if (questions.Any(q => string.IsNullOrWhiteSpace(q.Id)))
                throw new InvalidOperationException("Feedback form contains a question without an ID.");

            var duplicateQuestionId = questions
                .GroupBy(q => q.Id!.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateQuestionId != null)
                throw new InvalidOperationException($"Feedback form contains duplicate question ID '{duplicateQuestionId.Key}'.");

            var questionsById = questions.ToDictionary(q => q.Id!.Trim(), StringComparer.OrdinalIgnoreCase);
            var submittedAnswers = new Dictionary<string, SeminarFeedbackAnswerDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var answer in answers)
            {
                if (answer == null)
                    throw new ArgumentException("Feedback answers cannot contain null items.");

                var questionId = answer.QuestionId?.Trim();
                if (string.IsNullOrWhiteSpace(questionId))
                    throw new ArgumentException("Each feedback answer must contain questionId.");

                if (!questionsById.ContainsKey(questionId))
                    throw new ArgumentException($"Question '{questionId}' does not exist in this seminar feedback form.");

                if (!submittedAnswers.TryAdd(questionId, answer))
                    throw new ArgumentException($"Question '{questionId}' was answered more than once.");
            }

            var normalizedAnswers = new List<SeminarFeedbackAnswerDto>();

            foreach (var question in questions.OrderBy(q => q.OrderIndex))
            {
                var questionId = question.Id!.Trim();
                var questionType = question.Type?.Trim().ToLowerInvariant();

                if (questionType != "rating" && questionType != "text")
                    throw new InvalidOperationException($"Feedback form question '{questionId}' has unsupported type '{question.Type}'.");

                if (!submittedAnswers.TryGetValue(questionId, out var answer))
                {
                    if (question.IsRequired)
                        throw new ArgumentException($"Question '{questionId}' is required.");

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(answer.Type)
                    && !string.Equals(answer.Type.Trim(), questionType, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Question '{questionId}' must have type '{questionType}'.");

                if (questionType == "rating")
                {
                    if (!string.IsNullOrWhiteSpace(answer.Text))
                        throw new ArgumentException($"Rating question '{questionId}' cannot contain text.");

                    if (!answer.Rating.HasValue)
                    {
                        if (question.IsRequired)
                            throw new ArgumentException($"Question '{questionId}' requires a rating.");

                        continue;
                    }

                    var maxStar = question.MaxStar.HasValue && question.MaxStar.Value > 0
                        ? question.MaxStar.Value
                        : 5;

                    if (answer.Rating.Value < 1 || answer.Rating.Value > maxStar)
                        throw new ArgumentException($"Rating for question '{questionId}' must be between 1 and {maxStar}.");

                    normalizedAnswers.Add(new SeminarFeedbackAnswerDto
                    {
                        QuestionId = questionId,
                        OrderIndex = question.OrderIndex,
                        Type = "rating",
                        Rating = answer.Rating.Value,
                        Text = null
                    });

                    continue;
                }

                if (answer.Rating.HasValue)
                    throw new ArgumentException($"Text question '{questionId}' cannot contain a rating.");

                var text = answer.Text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (question.IsRequired)
                        throw new ArgumentException($"Question '{questionId}' requires a text answer.");

                    continue;
                }

                normalizedAnswers.Add(new SeminarFeedbackAnswerDto
                {
                    QuestionId = questionId,
                    OrderIndex = question.OrderIndex,
                    Type = "text",
                    Rating = null,
                    Text = text
                });
            }

            if (normalizedAnswers.Count == 0)
                throw new ArgumentException("Feedback must contain at least one valid answer.");

            return normalizedAnswers;
        }

        private static string SerializeDynamicFeedback(List<SeminarFeedbackAnswerDto> answers)
        {
            return JsonSerializer.Serialize(answers, FeedbackJsonOptions);
        }

        private static bool HasDynamicFeedbackPayload(SeminarFeedbackRequest request, out string jsonResult, out List<SeminarFeedbackAnswerDto>? answers)
        {
            jsonResult = string.Empty;
            answers = null;

            if (request == null)
                return false;

            if (request.Answers != null && request.Answers.Count > 0)
            {
                answers = request.Answers;
                jsonResult = JsonSerializer.Serialize(answers, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                return true;
            }

            if (request.FeedbackJson == null)
                return false;

            if (request.FeedbackJson is string str)
            {
                var trimmed = str.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    return false;

                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(trimmed, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        jsonResult = trimmed;
                        return true;
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("feedbackJson", out var innerFj))
                        {
                            if (innerFj.ValueKind == JsonValueKind.String)
                            {
                                var innerStr = innerFj.GetString()?.Trim() ?? string.Empty;
                                answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(innerStr, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                                jsonResult = innerStr;
                                return true;
                            }
                            else if (innerFj.ValueKind == JsonValueKind.Array)
                            {
                                jsonResult = innerFj.GetRawText();
                                answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(jsonResult, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to accepting trimmed string
                }

                jsonResult = trimmed;
                return true;
            }

            if (request.FeedbackJson is JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var inner = el.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(inner))
                    {
                        try
                        {
                            answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(inner, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        }
                        catch { }
                        jsonResult = inner;
                        return true;
                    }
                }
                else if (el.ValueKind == JsonValueKind.Array)
                {
                    jsonResult = el.GetRawText();
                    try
                    {
                        answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(jsonResult, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch { }
                    return true;
                }
                else if (el.ValueKind == JsonValueKind.Object)
                {
                    if (el.TryGetProperty("feedbackJson", out var innerFj))
                    {
                        if (innerFj.ValueKind == JsonValueKind.String)
                        {
                            jsonResult = innerFj.GetString()?.Trim() ?? string.Empty;
                        }
                        else
                        {
                            jsonResult = innerFj.GetRawText();
                        }
                        try
                        {
                            answers = JsonSerializer.Deserialize<List<SeminarFeedbackAnswerDto>>(jsonResult, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        }
                        catch { }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}