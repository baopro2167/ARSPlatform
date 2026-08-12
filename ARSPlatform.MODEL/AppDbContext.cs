using ARSPlatform.MODEL.Configurations;
using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.MODEL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Paper> Papers { get; set; } = null!;
        public DbSet<Seminar> Seminars { get; set; } = null!;
        public DbSet<SeminarParticipant> SeminarParticipants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new PaperConfiguration());
            modelBuilder.ApplyConfiguration(new SeminarConfiguration());

            modelBuilder.Entity<SeminarParticipant>(entity =>
            {
                entity.Property(e => e.InvitationStatus)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ParticipantEvaluation)
                    .HasMaxLength(255);

                entity.HasOne(e => e.Seminar)
                    .WithMany(e => e.SeminarParticipants)
                    .HasForeignKey(e => e.SeminarId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}