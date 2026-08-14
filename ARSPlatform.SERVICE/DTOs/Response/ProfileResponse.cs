using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ProfileResponse
    {
        public int UserId { get; set; }

        public string? PhoneNumber { get; set; }

        public string? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Address { get; set; }
    }
}
