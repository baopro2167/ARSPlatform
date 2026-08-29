using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PremiumPackageUpdateRequest
    {
        public string? Title { get; set; }

        public string? TargetRole { get; set; }

        public decimal? PriceVnd { get; set; }

        public string? BillingCycle { get; set; }

        public string[]? Features { get; set; }

        public bool? IsActive { get; set; }
    }
}
