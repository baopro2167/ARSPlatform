using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Mapping
{
    public class AutoMapperProfile : AutoMapper.Profile
    {
        public AutoMapperProfile()
        {
            // User
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src =>
                    src.UserRoles != null &&
                    src.UserRoles.Any() &&
                    src.UserRoles.First().Role != null
                        ? src.UserRoles.First().Role!.Name
                        : string.Empty));

            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

            // Paper
            CreateMap<PaperAuthor, PaperAuthorResponse>();

            CreateMap<Paper, PaperResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PaperId))
                .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.CreatorId))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    src.Creator != null ? src.Creator.FullName : string.Empty))
                .ForMember(dest => dest.AuthorOrcidId, opt => opt.MapFrom(src =>
                    src.Creator != null ? src.Creator.OrcidId : null))
                .ForMember(dest => dest.AuthorOrcidDisplayName, opt => opt.MapFrom(src =>
                    src.Creator != null ? src.Creator.OrcidDisplayName : null))
                .ForMember(dest => dest.AuthorIsOrcidVerified, opt => opt.MapFrom(src =>
                    src.Creator != null && src.Creator.IsOrcidVerified))
                .ForMember(dest => dest.Authors, opt => opt.MapFrom(src =>
                    src.PaperAuthors.OrderBy(author => author.AuthorOrder)));

            CreateMap<PaperCreateRequest, Paper>()
                .ForMember(dest => dest.PaperAuthors, opt => opt.Ignore());
            CreateMap<PaperUpdateRequest, Paper>()
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PaperAuthors, opt => opt.Ignore());

            // AuditLog
            CreateMap<AuditLog, AuditLogResponse>();
            CreateMap<AuditLogCreateRequest, AuditLog>()
                .ForMember(dest => dest.LogId, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.UtcNow));

            // CommentVote
            CreateMap<CommentVote, CommentVoteResponse>();
            CreateMap<CommentVoteCreateRequest, CommentVote>();
            CreateMap<CommentVoteUpdateRequest, CommentVote>();

            // DetailedEvaluation
            CreateMap<DetailedEvaluation, DetailedEvaluationResponse>()
                .ForMember(dest => dest.SpecializedEvaluation, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.SpecializedEvaluation)
                        ? new List<SpecializedEvaluationItemResponse>()
                        : JsonSerializer.Deserialize<List<SpecializedEvaluationItemResponse>>(
                            src.SpecializedEvaluation,
                            (JsonSerializerOptions?)null) ?? new List<SpecializedEvaluationItemResponse>()));

            CreateMap<DetailedEvaluationCreateRequest, DetailedEvaluation>()
                .ForMember(dest => dest.SpecializedEvaluation, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.SpecializedEvaluation ?? new List<SpecializedEvaluationItemRequest>(),
                        (JsonSerializerOptions?)null)));

            CreateMap<DetailedEvaluationUpdateRequest, DetailedEvaluation>()
                .ForMember(dest => dest.DetailedEvaluationId, opt => opt.Ignore())
                .ForMember(dest => dest.SpecializedEvaluation, opt =>
                {
                    opt.PreCondition(src => src.SpecializedEvaluation != null);
                    opt.MapFrom(src =>
                        JsonSerializer.Serialize(
                            src.SpecializedEvaluation,
                            (JsonSerializerOptions?)null));
                });

            // Follower
            CreateMap<Follower, FollowerResponse>()
                .ForMember(dest => dest.FollowerName, opt => opt.MapFrom(src =>
                    src.FollowerNavigation != null ? src.FollowerNavigation.FullName : string.Empty))
                .ForMember(dest => dest.FollowerEmail, opt => opt.MapFrom(src =>
                    src.FollowerNavigation != null ? src.FollowerNavigation.Email : string.Empty))
                .ForMember(dest => dest.FollowerAvatarUrl, opt => opt.MapFrom(src =>
                    src.FollowerNavigation != null ? src.FollowerNavigation.AvatarUrl : null))
                .ForMember(dest => dest.FollowedName, opt => opt.MapFrom(src =>
                    src.Followed != null ? src.Followed.FullName : string.Empty))
                .ForMember(dest => dest.FollowedEmail, opt => opt.MapFrom(src =>
                    src.Followed != null ? src.Followed.Email : string.Empty))
                .ForMember(dest => dest.FollowedAvatarUrl, opt => opt.MapFrom(src =>
                    src.Followed != null ? src.Followed.AvatarUrl : null));
            CreateMap<FollowerCreateRequest, Follower>();
            CreateMap<FollowerUpdateRequest, Follower>();

            // ForumComment
            CreateMap<ForumComment, ForumCommentResponse>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.AuthorAvatar, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvatarUrl : null));
            CreateMap<ForumCommentCreateRequest, ForumComment>();
            CreateMap<ForumCommentUpdateRequest, ForumComment>()
                .ForMember(dest => dest.ForumCommentId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // ForumPost
            CreateMap<ForumPost, ForumPostResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ForumPostId))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.AuthorAvatar, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvatarUrl : null))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Tags)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(
                            src.Tags,
                            (JsonSerializerOptions?)null) ?? new List<string>()))
                .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.LikeCount))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.ForumComments.Count))
                .ForMember(dest => dest.Views, opt => opt.MapFrom(src => src.ViewCount))
                .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.UserId));

            CreateMap<ForumPostCreateRequest, ForumPost>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.Tags ?? new List<string>(),
                        (JsonSerializerOptions?)null)))
                .ForMember(dest => dest.ForumPostId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
                .ForMember(dest => dest.ViewCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ForumComments, opt => opt.Ignore());

            // GroupMember
            CreateMap<GroupMember, GroupMemberResponse>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.FullName : null))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.Email : null))
                .ForMember(dest => dest.StudentAvatarUrl, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.AvatarUrl : null));
            CreateMap<GroupMemberCreateRequest, GroupMember>();
            CreateMap<GroupMemberUpdateRequest, GroupMember>()
                .ForMember(dest => dest.GroupMemberId, opt => opt.Ignore())
                .ForMember(dest => dest.JoinedAt, opt => opt.Ignore());

            // GuidanceProject
            CreateMap<GuidanceProject, GuidanceProjectResponse>()
                .ForMember(dest => dest.ResearchGroupName, opt => opt.MapFrom(src =>
                    src.ResearchGroup != null ? src.ResearchGroup.Name : null));
            CreateMap<GuidanceProjectCreateRequest, GuidanceProject>();
            CreateMap<GuidanceProjectUpdateRequest, GuidanceProject>()
                .ForMember(dest => dest.GuidanceProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // LearningMaterial
            CreateMap<LearningMaterial, LearningMaterialResponse>();
            CreateMap<LearningMaterialCreateRequest, LearningMaterial>();
            CreateMap<LearningMaterialUpdateRequest, LearningMaterial>()
                .ForMember(dest => dest.LearningMaterialId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // MajorField
            CreateMap<MajorField, MajorFieldResponse>();
            CreateMap<MajorFieldCreateRequest, MajorField>();
            CreateMap<MajorFieldUpdateRequest, MajorField>()
                .ForMember(dest => dest.MajorFieldId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // MembershipPackage
            CreateMap<MembershipPackage, MembershipPackageResponse>();
            CreateMap<MembershipPackageCreateRequest, MembershipPackage>();
            CreateMap<MembershipPackageUpdateRequest, MembershipPackage>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // MembershipPurchase
            CreateMap<MembershipPurchase, MembershipPurchaseResponse>();
            CreateMap<MembershipPurchaseCreateRequest, MembershipPurchase>();
            CreateMap<MembershipPurchaseUpdateRequest, MembershipPurchase>()
                .ForMember(dest => dest.PurchasesId, opt => opt.Ignore())
                .ForMember(dest => dest.PurchasedAt, opt => opt.Ignore());

            // Notification
            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationCreateRequest, Notification>();
            CreateMap<NotificationUpdateRequest, Notification>()
                .ForMember(dest => dest.NotificationId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // PhasedReport
            CreateMap<PhasedReport, PhasedReportResponse>()
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src =>
                    src.ResearchGroup != null ? src.ResearchGroup.Name : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
                    src.GroupMember != null && src.GroupMember.Student != null ? src.GroupMember.Student.FullName : null));
            CreateMap<PhasedReportCreateRequest, PhasedReport>();
            CreateMap<PhasedReportUpdateRequest, PhasedReport>()
                .ForMember(dest => dest.PhasedReportId, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore());

            // ProfessionalProfile
            CreateMap<ProfessionalProfile, ProfessionalProfileResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvatarUrl : null))
                .ForMember(dest => dest.OrcidId, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.OrcidId : null))
                .ForMember(dest => dest.IsOrcidVerified, opt => opt.MapFrom(src =>
                    src.User != null
                        ? (bool?)src.User.IsOrcidVerified
                        : null))
                .ForMember(dest => dest.OrcidVerifiedAt, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.OrcidVerifiedAt : null))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.IsAvailableForReview : null))
                .ForMember(dest => dest.SubFieldName, opt => opt.MapFrom(src =>
                    src.SubField != null ? src.SubField.Name : null))
                .ForMember(dest => dest.MajorFieldId, opt => opt.MapFrom(src =>
                    src.SubField != null ? src.SubField.MajorFieldId : null))
                .ForMember(dest => dest.MajorFieldName, opt => opt.MapFrom(src =>
                    src.SubField != null && src.SubField.MajorField != null
                        ? src.SubField.MajorField.Name
                        : null));

            CreateMap<ProfessionalProfileCreateRequest, ProfessionalProfile>();
            CreateMap<ProfessionalProfileUpdateRequest, ProfessionalProfile>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            // Profile
            CreateMap<ARSPlatform.MODEL.Entities.Profile, ProfileResponse>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvatarUrl : null))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserRoles != null && src.User.UserRoles.Any()
                        ? (src.User.UserRoles.FirstOrDefault()!.Role != null
                            ? src.User.UserRoles.FirstOrDefault()!.Role!.Name
                            : src.User.UserRoles.FirstOrDefault()!.UserRole1)
                        : null))
                .ForMember(dest => dest.OrcidId, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.OrcidId : null))
                .ForMember(dest => dest.IsOrcidVerified, opt => opt.MapFrom(src =>
                    src.User != null
                        ? (bool?)src.User.IsOrcidVerified
                        : null))
                .ForMember(dest => dest.OrcidVerifiedAt, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.OrcidVerifiedAt : null))
                .ForMember(dest => dest.Keywords, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Keywords)
                        ? Array.Empty<string>()
                        : JsonSerializer.Deserialize<string[]>(
                            src.Keywords,
                            (JsonSerializerOptions?)null) ?? Array.Empty<string>()));

            CreateMap<ProfileCreateRequest, ARSPlatform.MODEL.Entities.Profile>()
                .ForMember(dest => dest.Keywords, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.Keywords ?? Array.Empty<string>(),
                        (JsonSerializerOptions?)null)));

            CreateMap<ProfileUpdateRequest, ARSPlatform.MODEL.Entities.Profile>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Keywords, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.Keywords ?? Array.Empty<string>(),
                        (JsonSerializerOptions?)null)));

            // Report
            CreateMap<Report, ReportResponse>();
            CreateMap<ReportCreateRequest, Report>();
            CreateMap<ReportUpdateRequest, Report>()
                .ForMember(dest => dest.ReportId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // ResearchGroup
            CreateMap<ResearchGroup, ResearchGroupResponse>()
                .ForMember(dest => dest.LecturerName, opt => opt.MapFrom(src =>
                    src.Lecturer != null ? src.Lecturer.FullName : null))
                .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src =>
                    src.Topic != null ? src.Topic.Title : null))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src =>
                    src.GroupMembers != null ? src.GroupMembers.Count : 0))
                .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.GroupMembers));
            CreateMap<ResearchGroupCreateRequest, ResearchGroup>();
            CreateMap<ResearchGroupUpdateRequest, ResearchGroup>()
                .ForMember(dest => dest.ResearchGroupId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // ResearchTopic
            CreateMap<ResearchTopic, ResearchTopicResponse>()
                .ForMember(dest => dest.LecturerName, opt => opt.MapFrom(src =>
                    src.Lecturer != null ? src.Lecturer.FullName : null))
                .ForMember(dest => dest.GroupCount, opt => opt.MapFrom(src =>
                    src.ResearchGroups != null ? src.ResearchGroups.Count : 0))
                .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.ResearchGroups));
            CreateMap<ResearchTopicCreateRequest, ResearchTopic>();
            CreateMap<ResearchTopicUpdateRequest, ResearchTopic>()
                .ForMember(dest => dest.TopicId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // ReviewRequest
            CreateMap<ReviewRequest, ReviewRequestResponse>()
                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.FullName : null))
                .ForMember(dest => dest.ReviewerEmail, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.Email : null))
                .ForMember(dest => dest.ReviewerAvatarUrl, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.AvatarUrl : null));

            CreateMap<ReviewRequestCreateRequest, ReviewRequest>();
            CreateMap<ReviewRequestUpdateRequest, ReviewRequest>()
                .ForMember(dest => dest.ReviewRequestId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Seminar
            CreateMap<Seminar, SeminarResponse>()
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.SeminarParticipants))
                .ForMember(dest => dest.SubFieldName, opt => opt.MapFrom(src => src.SubField != null ? src.SubField.Name : null));

            CreateMap<SeminarCreateRequest, Seminar>();
            CreateMap<SeminarUpdateRequest, Seminar>()
                .ForMember(dest => dest.SeminarId, opt => opt.Ignore());

            // SeminarParticipant
            CreateMap<SeminarParticipant, SeminarParticipantResponse>()
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : src.InvitedEmail))
                .ForMember(dest => dest.Feedback, opt => opt.MapFrom(src => DeserializeSeminarFeedback(src.FeedbackJson)))
                .ForMember(dest => dest.ParticipantEvaluation, opt => opt.MapFrom(src => GetLegacyOverallComment(src.FeedbackJson)));

            CreateMap<SeminarParticipantCreateRequest, SeminarParticipant>()
                .ForMember(dest => dest.FeedbackJson, opt => opt.Ignore())
                .ForMember(dest => dest.FeedbackSubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FeedbackUpdatedAt, opt => opt.Ignore());

            CreateMap<SeminarParticipantUpdateRequest, SeminarParticipant>()
                .ForMember(dest => dest.SeminarParticipantId, opt => opt.Ignore())
                .ForMember(dest => dest.FeedbackJson, opt => opt.Ignore())
                .ForMember(dest => dest.FeedbackSubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FeedbackUpdatedAt, opt => opt.Ignore());

            // SharedMaterial
            CreateMap<SharedMaterial, SharedMaterialResponse>();
            CreateMap<SharedMaterialCreateRequest, SharedMaterial>();
            CreateMap<SharedMaterialUpdateRequest, SharedMaterial>()
                .ForMember(dest => dest.SharedMaterialId, opt => opt.Ignore())
                .ForMember(dest => dest.SharedAt, opt => opt.Ignore());

            // SubField
            CreateMap<SubField, SubFieldResponse>()
                .ForMember(dest => dest.MajorFieldName, opt => opt.MapFrom(src =>
                    src.MajorField != null ? src.MajorField.Name : null))
                .ForMember(dest => dest.GradingRubric, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.GradingRubric)
                        ? new List<GradingRubricCriterionResponse>()
                        : JsonSerializer.Deserialize<List<GradingRubricCriterionResponse>>(
                            src.GradingRubric,
                            (JsonSerializerOptions?)null) ?? new List<GradingRubricCriterionResponse>()));

            CreateMap<SubFieldCreateRequest, SubField>()
                .ForMember(dest => dest.GradingRubric, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.GradingRubric ?? new List<GradingRubricCriterionRequest>(),
                        (JsonSerializerOptions?)null)));

            CreateMap<SubFieldUpdateRequest, SubField>()
                .ForMember(dest => dest.SubFieldId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GradingRubric, opt =>
                {
                    opt.PreCondition(src => src.GradingRubric != null);
                    opt.MapFrom(src =>
                        JsonSerializer.Serialize(
                            src.GradingRubric,
                            (JsonSerializerOptions?)null));
                });

            // Transaction
            CreateMap<Transaction, TransactionResponse>();
            CreateMap<TransactionCreateRequest, Transaction>();
            CreateMap<TransactionUpdateRequest, Transaction>()
                .ForMember(dest => dest.TransactionId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // UserRole
            CreateMap<UserRole, UserRoleResponse>();
            CreateMap<UserRoleCreateRequest, UserRole>();
            CreateMap<UserRoleUpdateRequest, UserRole>()
                .ForMember(dest => dest.UserRoleId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // UserToken
            CreateMap<UserToken, UserTokenResponse>();
            CreateMap<UserTokenCreateRequest, UserToken>();
            CreateMap<UserTokenUpdateRequest, UserToken>()
                .ForMember(dest => dest.TokenId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Wallet
            CreateMap<Wallet, WalletResponse>();
            CreateMap<WalletCreateRequest, Wallet>();
            CreateMap<WalletUpdateRequest, Wallet>()
                .ForMember(dest => dest.WalletId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt =>
                {
                    opt.PreCondition(src => src.UserId.HasValue);
                    opt.MapFrom(src => src.UserId);
                })
                .ForMember(dest => dest.Balance, opt =>
                {
                    opt.PreCondition(src => src.Balance.HasValue);
                    opt.MapFrom(src => src.Balance);
                });

            // WithdrawalRequest
            CreateMap<WithdrawalRequest, WithdrawalRequestResponse>();
            CreateMap<WithdrawalRequestCreateRequest, WithdrawalRequest>()
                .ForMember(dest => dest.WithdrawalRequestId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "PENDING"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Medal & UserMedal
            CreateMap<Medal, MedalResponse>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Roles)
                        ? new List<string> { "All" }
                        : (src.Roles.Trim().StartsWith("[")
                            ? (JsonSerializer.Deserialize<List<string>>(src.Roles, (JsonSerializerOptions?)null) ?? new List<string>())
                            : new List<string> { src.Roles })))
                .ForMember(dest => dest.TitleVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.TitleVi) ? src.Title : src.TitleVi))
                .ForMember(dest => dest.DescriptionVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.DescriptionVi) ? src.Description : src.DescriptionVi));

            CreateMap<Medal, MedalSummaryDto>()
                .ForMember(dest => dest.TitleVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.TitleVi) ? src.Title : src.TitleVi))
                .ForMember(dest => dest.DescriptionVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.DescriptionVi) ? src.Description : src.DescriptionVi));

            CreateMap<MedalCreateRequest, Medal>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Id)
                        ? "medal-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                        : src.Id))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Code)
                        ? src.Title.Trim().ToUpper().Replace(" ", "_") + "_" + src.Tier.Trim().ToUpper()
                        : src.Code))
                .ForMember(dest => dest.TitleVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.TitleVi) ? src.Title : src.TitleVi))
                .ForMember(dest => dest.DescriptionVi, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.DescriptionVi) ? src.Description : src.DescriptionVi))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.Roles != null && src.Roles.Any()
                        ? JsonSerializer.Serialize(src.Roles, (JsonSerializerOptions?)null)
                        : "[\"All\"]"))
                .ForMember(dest => dest.CriteriaUnit, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.CriteriaUnit) ? "lần" : src.CriteriaUnit))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive ?? true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UserMedal, UserMedalResponse>()
                .ForMember(dest => dest.Medal, opt => opt.MapFrom(src => src.Medal))
                .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src =>
                    src.Medal == null || src.Medal.CriteriaThreshold <= 0
                        ? 0.0
                        : Math.Min(100.0, Math.Round((double)src.CurrentProgress / src.Medal.CriteriaThreshold * 100.0, 1))));
        }
        private static SeminarFeedbackContentResponse? DeserializeSeminarFeedback(string? feedbackJson)
        {
            if (string.IsNullOrWhiteSpace(feedbackJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<SeminarFeedbackContentResponse>(feedbackJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? GetLegacyOverallComment(string? feedbackJson)
        {
            return DeserializeSeminarFeedback(feedbackJson)?.OverallComment;
        }
    }
}