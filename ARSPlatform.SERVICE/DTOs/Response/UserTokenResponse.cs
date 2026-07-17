using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserTokenResponse
    {
        public int TokenId { get; set; }

        public int? UserId { get; set; }

        public string RefreshToken { get; set; }

        public string? DeviceInfo { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
