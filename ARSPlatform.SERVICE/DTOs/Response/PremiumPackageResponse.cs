using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PremiumPackageResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string TargetRole { get; set; } = string.Empty;

        public decimal PriceVnd { get; set; }

        public string BillingCycle { get; set; } = string.Empty;

        public string[] Features { get; set; } = Array.Empty<string>();

        public bool IsActive { get; set; }

        public int SubscriberCount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
