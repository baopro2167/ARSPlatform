using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class MembershipPackage
{
    public int PackageId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string TargetRole { get; set; } = "RESEARCHER";

    public string BillingCycle { get; set; } = "Monthly";

    public string? Features { get; set; }

    public bool IsActive { get; set; } = true;

    public int SubscriberCount { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual ICollection<MembershipPurchase> MembershipPurchases { get; set; } = new List<MembershipPurchase>();
}