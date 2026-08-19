using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Role
{
    public int RoleId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<RoleRequest> RoleRequests { get; set; } = new List<RoleRequest>();
}
