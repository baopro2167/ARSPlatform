using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class AnnualFeeUpdateRequest
    {
        [Required(ErrorMessage = "Target role is required.")]
        public string TargetRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price in VND is required.")]
        [Range(1, double.MaxValue, ErrorMessage = "Price VND must be a positive amount greater than 0.")]
        public decimal PriceVnd { get; set; }

        [Required(ErrorMessage = "Billing cycle is required.")]
        public string BillingCycle { get; set; } = "Annual";

        public List<string>? Features { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;
    }
}
