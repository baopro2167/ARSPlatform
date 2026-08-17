using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ARSPlatform.MODEL.Entities;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int AdminId { get; set; }

    public string AdminName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Target { get; set; } = null!;

    public string? TargetId { get; set; }

    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual User Admin { get; set; } = null!;
}