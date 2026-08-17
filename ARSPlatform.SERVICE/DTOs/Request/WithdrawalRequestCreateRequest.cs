using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class WithdrawalRequestCreateRequest
    {
        public int UserId { get; set; }

        public int WalletId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountNumber { get; set; } = null!;

        public string AccountName { get; set; } = null!;

        public decimal Amount { get; set; }

        public string? Note { get; set; }
    }
}