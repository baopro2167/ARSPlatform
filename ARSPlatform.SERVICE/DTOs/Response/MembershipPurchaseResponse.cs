using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MembershipPurchaseResponse
    {
        public int PurchasesId { get; set; }

        public int? UserId { get; set; }

        public int? PackageId { get; set; }

        public decimal PricePaid { get; set; }

        public DateTime? PurchasedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
