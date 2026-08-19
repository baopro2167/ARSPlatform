using System;

namespace ARSPlatform.MODEL.Entities;

public partial class RoleRequest
{
    public int RoleRequestId { get; set; }

    public int UserId { get; set; }

    public int RequestedRoleId { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string? Affiliation { get; set; }

    public string? Department { get; set; }

    public string ProofDocumentUrl { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string RequestType { get; set; } = null!;

    public string? Notes { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Role RequestedRole { get; set; } = null!;

    public virtual User? ReviewedByAdmin { get; set; }
}