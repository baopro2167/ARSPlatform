using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MembershipPackageResponse
    {
        public int PackageId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
