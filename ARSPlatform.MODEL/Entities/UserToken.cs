using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class UserToken
{
    public int TokenId { get; set; }

    public int? UserId { get; set; }

    public string RefreshToken { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
