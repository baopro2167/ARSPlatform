using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class ProfessionalProfile
{
    public int UserId { get; set; }

    public string? OrcidId { get; set; }

    public int? Hindex { get; set; }

    public int? TotalCitations { get; set; }

    public int? PublicationCount { get; set; }

    public string? SyncStatus { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? SubFieldId { get; set; }

    public decimal? ReviewFee { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual SubField? SubField { get; set; }
}