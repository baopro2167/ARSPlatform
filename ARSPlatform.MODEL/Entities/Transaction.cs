using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    // Payment fields (PayOS/VNPay)
    public string? PaymentOrderId { get; set; }

    public string? PaymentTransactionId { get; set; }

    public string? PaymentResponseCode { get; set; }

    [JsonIgnore]
    public virtual Wallet? Wallet { get; set; }
}
