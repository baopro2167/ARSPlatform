using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class WithdrawalRequestResponse
    {
        public int WithdrawalRequestId { get; set; }

        public int UserId { get; set; }

        public int WalletId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountNumber { get; set; } = null!;

        public string AccountName { get; set; } = null!;

        public decimal Amount { get; set; }

        public string Status { get; set; } = null!;

        public string? Note { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}