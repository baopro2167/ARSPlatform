using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    [JsonIgnore]
    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string? OrcidId { get; set; }

    public string? AvatarUrl { get; set; }

    public bool? IsEmailVerified { get; set; }

    public bool? IsActive { get; set; }

    public int? AccountTier { get; set; }

    public string? VerificationStatus { get; set; }

    public string? ProofDocumentUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsAvailableForReview { get; set; }

    public int? MaxSimultaneousPapers { get; set; }

    [JsonIgnore]
    public virtual ICollection<CommentVote> CommentVotes { get; set; }
        = new List<CommentVote>();

    [JsonIgnore]
    public virtual ICollection<ForumPost> ForumPosts { get; set; }
    = new List<ForumPost>();

    [JsonIgnore]
    public virtual ICollection<DetailedEvaluation> DetailedEvaluations { get; set; }
        = new List<DetailedEvaluation>();

    [JsonIgnore]
    public virtual ICollection<Follower> FollowerFolloweds { get; set; }
        = new List<Follower>();

    [JsonIgnore]
    public virtual ICollection<Follower> FollowerFollowerNavigations { get; set; }
        = new List<Follower>();

    [JsonIgnore]
    public virtual ICollection<ForumComment> ForumComments { get; set; }
        = new List<ForumComment>();

    [JsonIgnore]
    public virtual ICollection<GroupMember> GroupMembers { get; set; }
        = new List<GroupMember>();

    [JsonIgnore]
    public virtual ICollection<GuidanceProject> GuidanceProjectLecturers { get; set; }
        = new List<GuidanceProject>();

    [JsonIgnore]
    public virtual ICollection<GuidanceProject> GuidanceProjectStudents { get; set; }
        = new List<GuidanceProject>();

    [JsonIgnore]
    public virtual ICollection<LearningMaterial> LearningMaterials { get; set; }
        = new List<LearningMaterial>();

    [JsonIgnore]
    public virtual ICollection<MembershipPurchase> MembershipPurchases { get; set; }
        = new List<MembershipPurchase>();

    [JsonIgnore]
    public virtual ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();

    [JsonIgnore]
    public virtual ICollection<Paper> Papers { get; set; }
        = new List<Paper>();

    [JsonIgnore]
    public virtual ProfessionalProfile? ProfessionalProfile { get; set; }

    [JsonIgnore]
    public virtual Profile? Profile { get; set; }

    [JsonIgnore]
    public virtual ICollection<Report> Reports { get; set; }
        = new List<Report>();

    [JsonIgnore]
    public virtual ICollection<RoleRequest> RoleRequests { get; set; }
        = new List<RoleRequest>();

    [JsonIgnore]
    public virtual ICollection<ResearchGroup> ResearchGroups { get; set; }
        = new List<ResearchGroup>();

    [JsonIgnore]
    public virtual ICollection<ReviewRequest> ReviewRequests { get; set; }
        = new List<ReviewRequest>();

    [JsonIgnore]
    public virtual ICollection<SeminarParticipant> SeminarParticipants { get; set; }
        = new List<SeminarParticipant>();

    [JsonIgnore]
    public virtual ICollection<Seminar> Seminars { get; set; }
        = new List<Seminar>();

    [JsonIgnore]
    public virtual ICollection<SharedMaterial> SharedMaterialLecturers { get; set; }
        = new List<SharedMaterial>();

    [JsonIgnore]
    public virtual ICollection<SharedMaterial> SharedMaterialSharedWithColleagues { get; set; }
        = new List<SharedMaterial>();

    [JsonIgnore]
    public virtual ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    [JsonIgnore]
    public virtual ICollection<UserToken> UserTokens { get; set; }
        = new List<UserToken>();

    [JsonIgnore]
    public virtual Wallet? Wallet { get; set; }

    [JsonIgnore]
    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; }
        = new List<WithdrawalRequest>();
}