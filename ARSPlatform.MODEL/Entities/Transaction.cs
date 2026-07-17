using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int? WalletId { get; set; }

    public string? Type { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Wallet? Wallet { get; set; }
}
