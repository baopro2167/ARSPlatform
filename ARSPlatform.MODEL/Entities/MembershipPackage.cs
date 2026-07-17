using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class MembershipPackage
{
    public int PackageId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<MembershipPurchase> MembershipPurchases { get; set; } = new List<MembershipPurchase>();
}
