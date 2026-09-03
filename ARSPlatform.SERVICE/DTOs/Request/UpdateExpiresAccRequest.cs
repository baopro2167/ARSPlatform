using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UpdateExpiresAccRequest
    {
        public int UserId { get; set; }
        public DateTime ExpiresAcc { get; set; }
    }
}
