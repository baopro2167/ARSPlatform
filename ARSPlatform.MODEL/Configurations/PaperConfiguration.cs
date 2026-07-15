using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ARSPlatform.MODEL.Configurations
{
    public class PaperConfiguration : IEntityTypeConfiguration<Paper>
    {
        public void Configure(EntityTypeBuilder<Paper> builder)
        {
            builder.ToTable("Papers");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(p => p.Abstract)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(p => p.Doi)
                .HasMaxLength(100);

            builder.Property(p => p.FileUrl)
                .HasMaxLength(500);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(30);

            builder.HasOne(p => p.Author)
                .WithMany(u => u.Papers)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
