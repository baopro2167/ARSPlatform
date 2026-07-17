using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class TransactionCreateRequest
    {
        public int? WalletId { get; set; }

        public string? Type { get; set; }

        public decimal? Amount { get; set; }

        public string? Status { get; set; }

        public string? Description { get; set; }
    }
}
