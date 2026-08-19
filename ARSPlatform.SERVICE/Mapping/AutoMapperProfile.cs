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
            CreateMap<Paper, PaperResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PaperId))
                .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.CreatorId))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    src.Creator != null ? src.Creator.FullName : string.Empty));

            CreateMap<PaperCreateRequest, Paper>();
            CreateMap<PaperUpdateRequest, Paper>();

            // AuditLog
            CreateMap<AuditLog, AuditLogResponse>();
            CreateMap<AuditLogCreateRequest, AuditLog>();

            // CommentVote
            CreateMap<CommentVote, CommentVoteResponse>();
            CreateMap<CommentVoteCreateRequest, CommentVote>();
            CreateMap<CommentVoteUpdateRequest, CommentVote>();

            // DetailedEvaluation
            CreateMap<DetailedEvaluation, DetailedEvaluationResponse>();
            CreateMap<DetailedEvaluationCreateRequest, DetailedEvaluation>();
            CreateMap<DetailedEvaluationUpdateRequest, DetailedEvaluation>();

            // Follower
            CreateMap<Follower, FollowerResponse>();
            CreateMap<FollowerCreateRequest, Follower>();
            CreateMap<FollowerUpdateRequest, Follower>();

            // ForumComment
            CreateMap<ForumComment, ForumCommentResponse>();
            CreateMap<ForumCommentCreateRequest, ForumComment>();
            CreateMap<ForumCommentUpdateRequest, ForumComment>();

            // ForumPost
            CreateMap<ForumPost, ForumPostResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ForumPostId))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src =>
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
            CreateMap<GroupMember, GroupMemberResponse>();
            CreateMap<GroupMemberCreateRequest, GroupMember>();
            CreateMap<GroupMemberUpdateRequest, GroupMember>();

            // GuidanceProject
            CreateMap<GuidanceProject, GuidanceProjectResponse>();
            CreateMap<GuidanceProjectCreateRequest, GuidanceProject>();
            CreateMap<GuidanceProjectUpdateRequest, GuidanceProject>();

            // LearningMaterial
            CreateMap<LearningMaterial, LearningMaterialResponse>();
            CreateMap<LearningMaterialCreateRequest, LearningMaterial>();
            CreateMap<LearningMaterialUpdateRequest, LearningMaterial>();

            // MajorField
            CreateMap<MajorField, MajorFieldResponse>();
            CreateMap<MajorFieldCreateRequest, MajorField>();
            CreateMap<MajorFieldUpdateRequest, MajorField>();

            // MembershipPackage
            CreateMap<MembershipPackage, MembershipPackageResponse>();
            CreateMap<MembershipPackageCreateRequest, MembershipPackage>();
            CreateMap<MembershipPackageUpdateRequest, MembershipPackage>();

            // MembershipPurchase
            CreateMap<MembershipPurchase, MembershipPurchaseResponse>();
            CreateMap<MembershipPurchaseCreateRequest, MembershipPurchase>();
            CreateMap<MembershipPurchaseUpdateRequest, MembershipPurchase>();

            // Notification
            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationCreateRequest, Notification>();
            CreateMap<NotificationUpdateRequest, Notification>();

            // PhasedReport
            CreateMap<PhasedReport, PhasedReportResponse>();
            CreateMap<PhasedReportCreateRequest, PhasedReport>();
            CreateMap<PhasedReportUpdateRequest, PhasedReport>();

            // ProfessionalProfile - Mục 3A
            CreateMap<ProfessionalProfile, ProfessionalProfileResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvatarUrl : null))
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
            CreateMap<ProfessionalProfileUpdateRequest, ProfessionalProfile>();

            // Profile - F.2
            CreateMap<ARSPlatform.MODEL.Entities.Profile, ProfileResponse>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Email : null))
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
                .ForMember(dest => dest.Keywords, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(
                        src.Keywords ?? Array.Empty<string>(),
                        (JsonSerializerOptions?)null)));

            // Report
            CreateMap<Report, ReportResponse>();
            CreateMap<ReportCreateRequest, Report>();
            CreateMap<ReportUpdateRequest, Report>();

            // ResearchGroup
            CreateMap<ResearchGroup, ResearchGroupResponse>();
            CreateMap<ResearchGroupCreateRequest, ResearchGroup>();
            CreateMap<ResearchGroupUpdateRequest, ResearchGroup>();

            // ResearchTopic
            CreateMap<ResearchTopic, ResearchTopicResponse>();
            CreateMap<ResearchTopicCreateRequest, ResearchTopic>();
            CreateMap<ResearchTopicUpdateRequest, ResearchTopic>();

            // ReviewRequest - Mục 3A
            CreateMap<ReviewRequest, ReviewRequestResponse>()
                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.FullName : null))
                .ForMember(dest => dest.ReviewerEmail, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.Email : null))
                .ForMember(dest => dest.ReviewerAvatarUrl, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.AvatarUrl : null));

            CreateMap<ReviewRequestCreateRequest, ReviewRequest>();
            CreateMap<ReviewRequestUpdateRequest, ReviewRequest>();

            // Seminar
            CreateMap<Seminar, SeminarResponse>()
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.SeminarParticipants));

            CreateMap<SeminarCreateRequest, Seminar>();
            CreateMap<SeminarUpdateRequest, Seminar>();

            // SeminarParticipant
            CreateMap<SeminarParticipant, SeminarParticipantResponse>();
            CreateMap<SeminarParticipantCreateRequest, SeminarParticipant>();
            CreateMap<SeminarParticipantUpdateRequest, SeminarParticipant>();

            // SharedMaterial
            CreateMap<SharedMaterial, SharedMaterialResponse>();
            CreateMap<SharedMaterialCreateRequest, SharedMaterial>();
            CreateMap<SharedMaterialUpdateRequest, SharedMaterial>();

            // SubField - Mục 2
            CreateMap<SubField, SubFieldResponse>()
                .ForMember(dest => dest.MajorFieldName, opt => opt.MapFrom(src =>
                    src.MajorField != null ? src.MajorField.Name : null));

            CreateMap<SubFieldCreateRequest, SubField>();
            CreateMap<SubFieldUpdateRequest, SubField>();

            // Transaction
            CreateMap<Transaction, TransactionResponse>();
            CreateMap<TransactionCreateRequest, Transaction>();
            CreateMap<TransactionUpdateRequest, Transaction>();

            // UserRole
            CreateMap<UserRole, UserRoleResponse>();
            CreateMap<UserRoleCreateRequest, UserRole>();
            CreateMap<UserRoleUpdateRequest, UserRole>();

            // UserToken
            CreateMap<UserToken, UserTokenResponse>();
            CreateMap<UserTokenCreateRequest, UserToken>();
            CreateMap<UserTokenUpdateRequest, UserToken>();

            // Wallet
            CreateMap<Wallet, WalletResponse>();
            CreateMap<WalletCreateRequest, Wallet>();
            CreateMap<WalletUpdateRequest, Wallet>();

            // WithdrawalRequest
            CreateMap<WithdrawalRequest, WithdrawalRequestResponse>();
            CreateMap<WithdrawalRequestCreateRequest, WithdrawalRequest>();
        }
    }
}