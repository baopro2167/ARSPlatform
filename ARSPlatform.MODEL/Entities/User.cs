using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? OrcidId { get; set; }

        // Foreign Key
        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
    }
}
