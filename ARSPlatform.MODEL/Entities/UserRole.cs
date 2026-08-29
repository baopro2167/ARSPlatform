using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class UserRole
{
    public int UserRoleId { get; set; }

    public int? UserId { get; set; }

    public int? RoleId { get; set; }

    public string? UserRole1 { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Role? Role { get; set; }

    [JsonIgnore]
    public virtual User? User { get; set; }
}
