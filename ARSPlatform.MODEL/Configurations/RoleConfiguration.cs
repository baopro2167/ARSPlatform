using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ARSPlatform.MODEL.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            // Seed Roles
            builder.HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Researcher" },
                new Role { Id = 3, Name = "Reviewer" }
            );
        }
    }
}
