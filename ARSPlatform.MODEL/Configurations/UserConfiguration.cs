using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace ARSPlatform.MODEL.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(u => u.Username)
                .IsUnique();

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.OrcidId)
                .HasMaxLength(19);

            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed a default Admin user (password: Password123)
            builder.HasData(
                new User
                {
                    Id = Guid.Parse("d2b5b3a4-8b1e-4b45-b44c-1123456789ab"),
                    Username = "admin",
                    Email = "admin@arsplatform.com",
                    PasswordHash = "$2a$11$qRzN2Kk1k18kF4aF7G6HCuB9Z1qg2vW9uLg5X.R.G8.78t8hD8c8a", // BCrypt hash of "Password123"
                    FullName = "System Administrator",
                    RoleId = 1 // Admin
                }
            );
        }
    }
}
