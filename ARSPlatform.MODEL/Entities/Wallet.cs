using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int? UserId { get; set; }

    public decimal? Balance { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();

    [JsonIgnore]
    public virtual User? User { get; set; }

    [JsonIgnore]
    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; }
        = new List<WithdrawalRequest>();
}