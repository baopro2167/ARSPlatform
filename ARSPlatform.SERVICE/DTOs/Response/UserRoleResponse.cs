using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserRoleResponse
    {
        public int UserRoleId { get; set; }

        public int? UserId { get; set; }

        public int? RoleId { get; set; }

        public string? UserRole1 { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
