using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ProfileResponse
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? AcademicTitle { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Institution { get; set; }
        public string? Bio { get; set; }
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string? AvatarInitials { get; set; }
    }
}
