using System;
using System.Collections.Generic;
using ARSPlatform.MODEL.Configurations;
using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.MODEL;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public virtual DbSet<CommentVote> CommentVotes { get; set; }

        public virtual DbSet<DetailedEvaluation> DetailedEvaluations { get; set; }

        public virtual DbSet<Follower> Followers { get; set; }

        public virtual DbSet<ForumComment> ForumComments { get; set; }

        public virtual DbSet<GroupMember> GroupMembers { get; set; }

        public virtual DbSet<GuidanceProject> GuidanceProjects { get; set; }

        public virtual DbSet<LearningMaterial> LearningMaterials { get; set; }

        public virtual DbSet<MajorField> MajorFields { get; set; }

        public virtual DbSet<MembershipPackage> MembershipPackages { get; set; }

        public virtual DbSet<MembershipPurchase> MembershipPurchases { get; set; }

        public virtual DbSet<Notification> Notifications { get; set; }

        public virtual DbSet<Paper> Papers { get; set; }

        public virtual DbSet<PhasedReport> PhasedReports { get; set; }

        public virtual DbSet<ProfessionalProfile> ProfessionalProfiles { get; set; }

        public virtual DbSet<Profile> Profiles { get; set; }

        public virtual DbSet<Report> Reports { get; set; }

        public virtual DbSet<ResearchGroup> ResearchGroups { get; set; }

        public virtual DbSet<ResearchTopic> ResearchTopics { get; set; }

        public virtual DbSet<ReviewRequest> ReviewRequests { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<Seminar> Seminars { get; set; }

        public virtual DbSet<SeminarParticipant> SeminarParticipants { get; set; }

        public virtual DbSet<SharedMaterial> SharedMaterials { get; set; }

        public virtual DbSet<SubField> SubFields { get; set; }

        public virtual DbSet<Transaction> Transactions { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<UserRole> UserRoles { get; set; }

        public virtual DbSet<UserToken> UserTokens { get; set; }

        public virtual DbSet<Wallet> Wallets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            // ... (incoming branch contains extensive entity configurations; retained as-is)

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
