using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UpdateExpiresAtRequest
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
