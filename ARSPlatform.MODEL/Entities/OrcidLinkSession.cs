using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class OrcidLinkSession
{
    public int OrcidLinkSessionId { get; set; }

    public string StateHash { get; set; } = null!;

    public string? TicketHash { get; set; }

    public string Context { get; set; } = null!;

    public int? UserId { get; set; }

    public string? AuthenticatedOrcidId { get; set; }

    public string? DisplayName { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? AuthenticatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? FailureCode { get; set; }

    [JsonIgnore]
    public virtual User? User { get; set; }
}