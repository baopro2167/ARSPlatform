using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class MembershipPackageUpdateRequest
    {
        public int PackageId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public string? Description { get; set; }
    }
}
