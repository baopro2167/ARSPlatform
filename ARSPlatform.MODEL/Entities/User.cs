using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

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

    public virtual ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();

    public virtual ICollection<DetailedEvaluation> DetailedEvaluations { get; set; } = new List<DetailedEvaluation>();

    public virtual ICollection<Follower> FollowerFolloweds { get; set; } = new List<Follower>();

    public virtual ICollection<Follower> FollowerFollowerNavigations { get; set; } = new List<Follower>();

    public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<GuidanceProject> GuidanceProjectLecturers { get; set; } = new List<GuidanceProject>();

    public virtual ICollection<GuidanceProject> GuidanceProjectStudents { get; set; } = new List<GuidanceProject>();

    public virtual ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();

    public virtual ICollection<MembershipPurchase> MembershipPurchases { get; set; } = new List<MembershipPurchase>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();

    public virtual ProfessionalProfile? ProfessionalProfile { get; set; }

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<ResearchGroup> ResearchGroups { get; set; } = new List<ResearchGroup>();

    public virtual ICollection<ReviewRequest> ReviewRequests { get; set; } = new List<ReviewRequest>();

    public virtual ICollection<SeminarParticipant> SeminarParticipants { get; set; } = new List<SeminarParticipant>();

    public virtual ICollection<Seminar> Seminars { get; set; } = new List<Seminar>();

    public virtual ICollection<SharedMaterial> SharedMaterialLecturers { get; set; } = new List<SharedMaterial>();

    public virtual ICollection<SharedMaterial> SharedMaterialSharedWithColleagues { get; set; } = new List<SharedMaterial>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();

    public virtual Wallet? Wallet { get; set; }
}