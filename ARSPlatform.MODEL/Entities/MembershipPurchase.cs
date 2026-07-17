using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class MembershipPurchase
{
    public int PurchasesId { get; set; }

    public int? UserId { get; set; }

    public int? PackageId { get; set; }

    public decimal PricePaid { get; set; }

    public DateTime? PurchasedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public virtual MembershipPackage? Package { get; set; }

    public virtual User? User { get; set; }
}
