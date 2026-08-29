using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class AnnualFeeResponse
    {
        public int Id { get; set; }

        public string TargetRole { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public decimal PriceVnd { get; set; }

        public string BillingCycle { get; set; } = "Annual";

        public List<string> Features { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }
    }
}
