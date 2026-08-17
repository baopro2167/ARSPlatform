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
            // =========================================================
            // User Mapping
            // =========================================================

            CreateMap<User, UserResponse>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.UserId))
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src =>
                        src.UserRoles != null &&
                        src.UserRoles.Any() &&
                        src.UserRoles.First().Role != null
                            ? src.UserRoles.First().Role!.Name
                            : string.Empty));

            CreateMap<RegisterRequest, User>()
                .ForMember(
                    dest => dest.PasswordHash,
                    opt => opt.Ignore())
                .ForMember(
                    dest => dest.UserRoles,
                    opt => opt.Ignore());


            // =========================================================
            // Paper Mapping
            // =========================================================

            CreateMap<Paper, PaperResponse>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.PaperId))
                .ForMember(
                    dest => dest.AuthorId,
                    opt => opt.MapFrom(src => src.CreatorId))
                .ForMember(
                    dest => dest.AuthorName,
                    opt => opt.MapFrom(src =>
                        src.Creator != null
                            ? src.Creator.FullName
                            : string.Empty));

            CreateMap<PaperCreateRequest, Paper>();
            CreateMap<PaperUpdateRequest, Paper>();


            // =========================================================
            // Generated Mappings
            // =========================================================

            CreateMap<AuditLog, AuditLogResponse>();
            CreateMap<AuditLogCreateRequest, AuditLog>();

            CreateMap<CommentVote, CommentVoteResponse>();
            CreateMap<CommentVoteCreateRequest, CommentVote>();
            CreateMap<CommentVoteUpdateRequest, CommentVote>();

            CreateMap<DetailedEvaluation, DetailedEvaluationResponse>();
            CreateMap<DetailedEvaluationCreateRequest, DetailedEvaluation>();
            CreateMap<DetailedEvaluationUpdateRequest, DetailedEvaluation>();

            CreateMap<Follower, FollowerResponse>();
            CreateMap<FollowerCreateRequest, Follower>();
            CreateMap<FollowerUpdateRequest, Follower>();

            CreateMap<ForumComment, ForumCommentResponse>();
            CreateMap<ForumCommentCreateRequest, ForumComment>();
            CreateMap<ForumCommentUpdateRequest, ForumComment>();

            CreateMap<GroupMember, GroupMemberResponse>();
            CreateMap<GroupMemberCreateRequest, GroupMember>();
            CreateMap<GroupMemberUpdateRequest, GroupMember>();

            CreateMap<GuidanceProject, GuidanceProjectResponse>();
            CreateMap<GuidanceProjectCreateRequest, GuidanceProject>();
            CreateMap<GuidanceProjectUpdateRequest, GuidanceProject>();

            CreateMap<LearningMaterial, LearningMaterialResponse>();
            CreateMap<LearningMaterialCreateRequest, LearningMaterial>();
            CreateMap<LearningMaterialUpdateRequest, LearningMaterial>();

            CreateMap<MajorField, MajorFieldResponse>();
            CreateMap<MajorFieldCreateRequest, MajorField>();
            CreateMap<MajorFieldUpdateRequest, MajorField>();

            CreateMap<MembershipPackage, MembershipPackageResponse>();
            CreateMap<MembershipPackageCreateRequest, MembershipPackage>();
            CreateMap<MembershipPackageUpdateRequest, MembershipPackage>();

            CreateMap<MembershipPurchase, MembershipPurchaseResponse>();
            CreateMap<MembershipPurchaseCreateRequest, MembershipPurchase>();
            CreateMap<MembershipPurchaseUpdateRequest, MembershipPurchase>();

            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationCreateRequest, Notification>();
            CreateMap<NotificationUpdateRequest, Notification>();

            CreateMap<PhasedReport, PhasedReportResponse>();
            CreateMap<PhasedReportCreateRequest, PhasedReport>();
            CreateMap<PhasedReportUpdateRequest, PhasedReport>();

            CreateMap<ProfessionalProfile, ProfessionalProfileResponse>();
            CreateMap<ProfessionalProfileCreateRequest, ProfessionalProfile>();
            CreateMap<ProfessionalProfileUpdateRequest, ProfessionalProfile>();

            CreateMap<ARSPlatform.MODEL.Entities.Profile, ProfileResponse>();
            CreateMap<ProfileCreateRequest, ARSPlatform.MODEL.Entities.Profile>();
            CreateMap<ProfileUpdateRequest, ARSPlatform.MODEL.Entities.Profile>();

            CreateMap<Report, ReportResponse>();
            CreateMap<ReportCreateRequest, Report>();
            CreateMap<ReportUpdateRequest, Report>();

            CreateMap<ResearchGroup, ResearchGroupResponse>();
            CreateMap<ResearchGroupCreateRequest, ResearchGroup>();
            CreateMap<ResearchGroupUpdateRequest, ResearchGroup>();

            CreateMap<ResearchTopic, ResearchTopicResponse>();
            CreateMap<ResearchTopicCreateRequest, ResearchTopic>();
            CreateMap<ResearchTopicUpdateRequest, ResearchTopic>();

            CreateMap<ReviewRequest, ReviewRequestResponse>();
            CreateMap<ReviewRequestCreateRequest, ReviewRequest>();
            CreateMap<ReviewRequestUpdateRequest, ReviewRequest>();


            // =========================================================
            // Seminar Mapping
            // =========================================================

            CreateMap<Seminar, SeminarResponse>()
                .ForMember(
                    dest => dest.Participants,
                    opt => opt.MapFrom(
                        src => src.SeminarParticipants));

            CreateMap<SeminarCreateRequest, Seminar>();
            CreateMap<SeminarUpdateRequest, Seminar>();


            // =========================================================
            // SeminarParticipant Mapping
            // =========================================================

            CreateMap<SeminarParticipant, SeminarParticipantResponse>();
            CreateMap<SeminarParticipantCreateRequest, SeminarParticipant>();
            CreateMap<SeminarParticipantUpdateRequest, SeminarParticipant>();


            // =========================================================
            // SharedMaterial
            // =========================================================

            CreateMap<SharedMaterial, SharedMaterialResponse>();
            CreateMap<SharedMaterialCreateRequest, SharedMaterial>();
            CreateMap<SharedMaterialUpdateRequest, SharedMaterial>();


            // =========================================================
            // SubField
            // =========================================================

            CreateMap<SubField, SubFieldResponse>();
            CreateMap<SubFieldCreateRequest, SubField>();
            CreateMap<SubFieldUpdateRequest, SubField>();


            // =========================================================
            // Transaction
            // =========================================================

            CreateMap<Transaction, TransactionResponse>();
            CreateMap<TransactionCreateRequest, Transaction>();
            CreateMap<TransactionUpdateRequest, Transaction>();


            // =========================================================
            // UserRole
            // =========================================================

            CreateMap<UserRole, UserRoleResponse>();
            CreateMap<UserRoleCreateRequest, UserRole>();
            CreateMap<UserRoleUpdateRequest, UserRole>();


            // =========================================================
            // UserToken
            // =========================================================

            CreateMap<UserToken, UserTokenResponse>();
            CreateMap<UserTokenCreateRequest, UserToken>();
            CreateMap<UserTokenUpdateRequest, UserToken>();


            // =========================================================
            // Wallet
            // =========================================================

            CreateMap<Wallet, WalletResponse>();
            CreateMap<WalletCreateRequest, Wallet>();
            CreateMap<WalletUpdateRequest, Wallet>();


            // =========================================================
            // WithdrawalRequest
            // =========================================================

            CreateMap<WithdrawalRequest, WithdrawalRequestResponse>();

            CreateMap<WithdrawalRequestCreateRequest, WithdrawalRequest>();
        }
    }
}