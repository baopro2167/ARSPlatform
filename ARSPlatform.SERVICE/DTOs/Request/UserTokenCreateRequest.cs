using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UserTokenCreateRequest
    {
        public int TokenId { get; set; }

        public int? UserId { get; set; }

        public string RefreshToken { get; set; }

        public string? DeviceInfo { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
