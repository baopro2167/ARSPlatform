using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class TransactionResponse
    {
        public int TransactionId { get; set; }

        public int? WalletId { get; set; }

        public string? Type { get; set; }

        public decimal? Amount { get; set; }

        public string? Status { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        // AnnualFee purchase
        public int? AnnualFeeId { get; set; }

        public int? UserId { get; set; }

        public string? PaymentDescription { get; set; }

        public string? PaymentOrderId { get; set; }

        public string? PaymentResponseCode { get; set; }
    }
}
