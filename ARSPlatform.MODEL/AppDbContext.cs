using System;
using System.Collections.Generic;
using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.MODEL;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CommentVote> CommentVotes { get; set; }

    public virtual DbSet<DetailedEvaluation> DetailedEvaluations { get; set; }

    public virtual DbSet<Follower> Followers { get; set; }

    public virtual DbSet<ForumComment> ForumComments { get; set; }

    public virtual DbSet<ForumPost> ForumPosts { get; set; }

    public virtual DbSet<ForumPostLike> ForumPostLikes { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<GuidanceProject> GuidanceProjects { get; set; }

    public virtual DbSet<LearningMaterial> LearningMaterials { get; set; }

    public virtual DbSet<MajorField> MajorFields { get; set; }

    public virtual DbSet<MembershipPackage> MembershipPackages { get; set; }

    public virtual DbSet<MembershipPurchase> MembershipPurchases { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OrcidLinkSession> OrcidLinkSessions { get; set; }

    public virtual DbSet<Paper> Papers { get; set; }

    public virtual DbSet<PhasedReport> PhasedReports { get; set; }

    public virtual DbSet<ProfessionalProfile> ProfessionalProfiles { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ResearchGroup> ResearchGroups { get; set; }

    public virtual DbSet<ResearchTopic> ResearchTopics { get; set; }

    public virtual DbSet<ReviewRequest> ReviewRequests { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleRequest> RoleRequests { get; set; }

    public virtual DbSet<Seminar> Seminars { get; set; }

    public virtual DbSet<SeminarParticipant> SeminarParticipants { get; set; }

    public virtual DbSet<SharedMaterial> SharedMaterials { get; set; }

    public virtual DbSet<SubField> SubFields { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasKey(e => e.LogId);

            entity.HasIndex(e => e.AdminId, "IX_AuditLog_AdminId");

            entity.HasIndex(e => e.Timestamp, "IX_AuditLog_Timestamp").IsDescending();

            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.AdminName).HasMaxLength(255);
            entity.Property(e => e.Target)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TargetId).HasMaxLength(100);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Admin).WithMany()
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLog_User");
        });

        modelBuilder.Entity<CommentVote>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ForumCommentId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ForumComment).WithMany(p => p.CommentVotes)
                .HasForeignKey(d => d.ForumCommentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommentVo__Forum__02FC7413");

            entity.HasOne(d => d.User).WithMany(p => p.CommentVotes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__CommentVo__UserI__02084FDA");
        });

        modelBuilder.Entity<ForumPostLike>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ForumPostId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ForumPost).WithMany(p => p.ForumPostLikes)
                .HasForeignKey(d => d.ForumPostId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ForumPostLikes_ForumPost");

            entity.HasOne(d => d.User).WithMany(p => p.ForumPostLikes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ForumPostLikes_User");
        });

        modelBuilder.Entity<DetailedEvaluation>(entity =>
        {
            entity.HasIndex(e => e.ReviewRequestId, "UQ__Detailed__B13067656CE733B3").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FinalDecision)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NotesFormatting).HasMaxLength(255);
            entity.Property(e => e.NotesLiterature).HasMaxLength(255);
            entity.Property(e => e.NotesMethodology).HasMaxLength(255);
            entity.Property(e => e.NotesOriginality).HasMaxLength(255);
            entity.Property(e => e.NotesResults).HasMaxLength(255);

            entity.HasOne(d => d.ReviewRequest).WithOne(p => p.DetailedEvaluation)
                .HasForeignKey<DetailedEvaluation>(d => d.ReviewRequestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__DetailedE__Revie__75A278F5");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.DetailedEvaluations)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("FK__DetailedE__Revie__76969D2E");
        });

        modelBuilder.Entity<Follower>(entity =>
        {
            entity.HasKey(e => new { e.FollowerId, e.FollowedId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Followed).WithMany(p => p.FollowerFolloweds)
                .HasForeignKey(d => d.FollowedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Followers__Follo__5812160E");

            entity.HasOne(d => d.FollowerNavigation).WithMany(p => p.FollowerFollowerNavigations)
                .HasForeignKey(d => d.FollowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Followers__Follo__571DF1D5");
        });

        modelBuilder.Entity<ForumComment>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_ForumComments_update"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpvoteCount).HasDefaultValue(0);

            entity.HasOne(d => d.Paper).WithMany(p => p.ForumComments)
                .HasForeignKey(d => d.PaperId)
                .HasConstraintName("FK__ForumComm__Paper__7B5B524B");

            entity.HasOne(d => d.Reply).WithMany(p => p.InverseReply)
                .HasForeignKey(d => d.ReplyId)
                .HasConstraintName("FK__ForumComm__Reply__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.ForumComments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ForumComm__UserI__7A672E12");
        });

        modelBuilder.Entity<ForumPost>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithMany(p => p.ForumPosts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ForumPosts__UserId__7D439ABD");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.Property(e => e.ActivityStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ResearchGroup).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.ResearchGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__GroupMemb__Resea__14270015");

            entity.HasOne(d => d.Student).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__GroupMemb__Stude__151B244E");
        });

        modelBuilder.Entity<GuidanceProject>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Lecturer).WithMany(p => p.GuidanceProjectLecturers)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__GuidanceP__Lectu__1DB06A4F");

            entity.HasOne(d => d.Student).WithMany(p => p.GuidanceProjectStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__GuidanceP__Stude__1F98B2C1");

            entity.HasOne(d => d.ResearchGroup).WithMany(p => p.GuidanceProjects)
                .HasForeignKey(d => d.ResearchGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LearningMaterial>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Lecturer).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__LearningM__Lectu__18EBB532");

            entity.HasOne(d => d.SubField).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.SubFieldId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__LearningM__SubFi__1AD3FDA4");
        });

        modelBuilder.Entity<MajorField>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<MembershipPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(15, 2)");
        });

        modelBuilder.Entity<MembershipPurchase>(entity =>
        {
            entity.HasKey(e => e.PurchasesId);

            entity.Property(e => e.PricePaid).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.PurchasedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Package).WithMany(p => p.MembershipPurchases)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__Membershi__Packa__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.MembershipPurchases)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Membershi__UserI__693CA210");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Notificat__UserI__3E1D39E1");
        });

        modelBuilder.Entity<OrcidLinkSession>(entity =>
        {
            entity.ToTable("OrcidLinkSessions", tb =>
            {
                tb.HasCheckConstraint(
                    "CK_OrcidLinkSessions_Context",
                    "[Context] IN ('REGISTRATION', 'ACCOUNT_LINK')");

                tb.HasCheckConstraint(
                    "CK_OrcidLinkSessions_Status",
                    "[Status] IN ('PENDING', 'AUTHENTICATED', 'COMPLETED', 'FAILED')");

                tb.HasCheckConstraint(
                    "CK_OrcidLinkSessions_AccountLinkUser",
                    "[Context] <> 'ACCOUNT_LINK' OR [UserId] IS NOT NULL");

                tb.HasCheckConstraint(
                    "CK_OrcidLinkSessions_RegistrationUser",
                    "[Context] <> 'REGISTRATION' OR [UserId] IS NULL");
            });

            entity.HasKey(e => e.OrcidLinkSessionId);

            entity.HasIndex(
                e => e.ExpiresAt,
                "IX_OrcidLinkSessions_ExpiresAt");

            entity.HasIndex(
                    e => e.StateHash,
                    "UX_OrcidLinkSessions_StateHash")
                .IsUnique();

            entity.HasIndex(
                    e => e.TicketHash,
                    "UX_OrcidLinkSessions_TicketHash")
                .HasFilter("[TicketHash] IS NOT NULL")
                .IsUnique();

            entity.HasIndex(
                    e => e.UserId,
                    "IX_OrcidLinkSessions_UserId")
                .HasFilter("[UserId] IS NOT NULL");

            entity.Property(e => e.AuthenticatedOrcidId)
                .HasMaxLength(19)
                .IsUnicode(false);

            entity.Property(e => e.Context)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(255);

            entity.Property(e => e.FailureCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.StateHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");

            entity.Property(e => e.TicketHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrcidLinkSessions_User");
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Papers_update"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsOpenAccess).HasDefaultValue(false);
            entity.Property(e => e.Issn).HasDefaultValue(false);
            entity.Property(e => e.Quartile)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Creator).WithMany(p => p.Papers)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Papers__CreatorI__4F7CD00D");

            entity.HasOne(d => d.SubField).WithMany(p => p.Papers)
                .HasForeignKey(d => d.SubFieldId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Papers__SubField__5441852A");
        });

        modelBuilder.Entity<PhasedReport>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_PhasedReports_update"));

            entity.Property(e => e.CapacityEvaluation).HasMaxLength(255);
            entity.Property(e => e.FinalOutcomeEvaluation).HasMaxLength(255);
            entity.Property(e => e.LectureFeedback).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Status).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.MilestoneTitle).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.GroupMember).WithMany(p => p.PhasedReports)
                .HasForeignKey(d => d.GroupMemberId)
                .HasConstraintName("FK__PhasedRep__Group__236943A5");

            entity.HasOne(d => d.ResearchGroup).WithMany(p => p.PhasedReports)
                .HasForeignKey(d => d.ResearchGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__PhasedRep__Resea__22751F6C");
        });

        modelBuilder.Entity<ProfessionalProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable(tb => tb.HasTrigger("trg_ProfessionalProfiles_update"));

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Hindex)
                .HasDefaultValue(0)
                .HasColumnName("HIndex");
            entity.Property(e => e.OrcidId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PublicationCount).HasDefaultValue(0);
            entity.Property(e => e.SyncStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalCitations).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ReviewFee).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.User).WithOne(p => p.ProfessionalProfile)
                .HasForeignKey<ProfessionalProfile>(d => d.UserId)
                .HasConstraintName("FK__Professio__UserI__4222D4EF");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Profile");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.DateOfBirth).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.PhoneNumber).HasMaxLength(255);

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("FK__Profile__UserId__31B762FC");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Reports_update"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TargetType).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ViolationNotes).HasMaxLength(255);

            entity.HasOne(d => d.Reporter).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ReporterId)
                .HasConstraintName("FK__Reports__Reporte__06CD04F7");
        });

        modelBuilder.Entity<ResearchGroup>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ResearchGroups)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__ResearchG__Lectu__0F624AF8");

            entity.HasOne(d => d.Topic).WithMany(p => p.ResearchGroups)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__ResearchG__Topic__10566F31");
        });

        modelBuilder.Entity<ResearchTopic>(entity =>
        {
            entity.HasKey(e => e.TopicId);

            entity.ToTable(tb => tb.HasTrigger("trg_ResearchTopics_update"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ResearchTopics)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReviewRequest>(entity =>
        {
            entity.ToTable("ReviewRequest");

            entity.Property(e => e.Airecommended).HasColumnName("AIRecommended");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Fee)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Paper).WithMany(p => p.ReviewRequests)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ReviewReq__Paper__6EF57B66");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewRequests)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("FK__ReviewReq__Revie__6FE99F9F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasData(
                new Role { RoleId = 4, Name = "Lecturer", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 5, Name = "Graduate Student", CreatedAt = DateTime.UtcNow }
            );
        });

        modelBuilder.Entity<RoleRequest>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.RequestType).HasMaxLength(50);

            entity.HasOne(d => d.User)
                .WithMany(p => p.RoleRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__RoleRequests__UserId");

            entity.HasOne(d => d.RequestedRole)
                .WithMany(p => p.RoleRequests)
                .HasForeignKey(d => d.RequestedRoleId)
                .HasConstraintName("FK__RoleRequests__RequestedRoleId");
        });

        modelBuilder.Entity<Seminar>(entity =>
        {
            entity.Property(e => e.IsReminderSent).HasDefaultValue(false);
            entity.Property(e => e.MaxParticipants).HasDefaultValue(0);
            entity.Property(e => e.OnlineLink).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Organizer).WithMany(p => p.Seminars)
                .HasForeignKey(d => d.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Seminars__Organi__2CF2ADDF");
        });

        modelBuilder.Entity<SeminarParticipant>(entity =>
        {
            entity.Property(e => e.InvitationStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParticipantEvaluation).HasMaxLength(255);

            entity.HasOne(d => d.Seminar).WithMany(p => p.SeminarParticipants)
                .HasForeignKey(d => d.SeminarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__SeminarPa__Semin__42E1EEFE");

            entity.HasOne(d => d.User).WithMany(p => p.SeminarParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SeminarPa__UserI__43D61337");
        });

        modelBuilder.Entity<SharedMaterial>(entity =>
        {
            entity.Property(e => e.SharedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Lecturer).WithMany(p => p.SharedMaterialLecturers)
                .HasForeignKey(d => d.LecturerId)
                .HasConstraintName("FK__SharedMat__Lectu__2739D489");

            entity.HasOne(d => d.Paper).WithMany(p => p.SharedMaterials)
                .HasForeignKey(d => d.PaperId)
                .HasConstraintName("FK__SharedMat__Paper__282DF8C2");

            entity.HasOne(d => d.SharedWithColleague).WithMany(p => p.SharedMaterialSharedWithColleagues)
                .HasForeignKey(d => d.SharedWithColleagueId)
                .HasConstraintName("FK__SharedMat__Share__29221CFB");
        });

        modelBuilder.Entity<SubField>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.MajorField).WithMany(p => p.SubFields)
                .HasForeignKey(d => d.MajorFieldId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__SubFields__Major__4BAC3F29");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Wallet).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Transacti__Walle__619B8048");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", tb => tb.HasTrigger("trg_User_update"));

            entity.HasIndex(e => e.GoogleId, "UX_User_GoogleId").HasFilter("[GoogleId] IS NOT NULL").IsUnique();

            entity.HasIndex(e => e.OrcidId, "UX_User_OrcidId").HasFilter("[OrcidId] IS NOT NULL").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__User__A9D105342E34C60E").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.IsOrcidVerified).HasDefaultValue(false);
            entity.Property(e => e.OrcidId)
                .HasMaxLength(19)
                .IsUnicode(false);
            entity.Property(e => e.OrcidVerifiedAt)
                .HasColumnType("datetime2(7)");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

            entity.HasIndex(
                    e => new { e.UserId, e.RoleId },
                    "UX_UserRole_UserId_RoleId")
                .HasFilter("[UserId] IS NOT NULL AND [RoleId] IS NOT NULL")
                .IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserRole1)
                .HasMaxLength(255)
                .HasColumnName("UserRole");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__UserRole__RoleId__3A4CA8FD");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__UserRole__UserId__395884C4");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.HasIndex(e => e.RefreshToken, "UQ__UserToke__DEA298DA118F54B9").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__UserToken__UserI__3587F3E0");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Wallets_update"));

            entity.HasIndex(e => e.UserId, "UQ__Wallets__1788CC4D1AA07263").IsUnique();

            entity.Property(e => e.Balance)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(15, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Wallets__UserId__5CD6CB2B");
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.ToTable("WithdrawalRequests");
            entity.HasKey(e => e.WithdrawalRequestId);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Amount).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.BankName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50).IsUnicode(false).HasDefaultValue("PENDING");

            entity.HasOne(d => d.User).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WithdrawalRequests_User");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WithdrawalRequests_Wallet");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}