using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? OrcidId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
