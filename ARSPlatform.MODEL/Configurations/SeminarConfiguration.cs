using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ARSPlatform.MODEL.Configurations;

public class SeminarConfiguration : IEntityTypeConfiguration<Seminar>
{
    public void Configure(EntityTypeBuilder<Seminar> builder)
    {
        builder.ToTable("Seminars");

        builder.HasKey(s => s.SeminarId);

        builder.Property(s => s.AiSummary)
               .HasColumnName("aiSummary");

        builder.Property(s => s.Feedback)
               .HasColumnName("feedback");
    }
}
