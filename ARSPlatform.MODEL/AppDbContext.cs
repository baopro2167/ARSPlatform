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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new PaperConfiguration());
            modelBuilder.ApplyConfiguration(new SeminarConfiguration());
        }
    }
}