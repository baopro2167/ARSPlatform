using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UserRoleUpdateRequest
    {
        public int? UserId { get; set; }

        public int? RoleId { get; set; }

        public string? UserRole1 { get; set; }
    }
}
