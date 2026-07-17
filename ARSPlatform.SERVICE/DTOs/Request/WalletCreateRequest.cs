using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class WalletCreateRequest
    {
        public int? UserId { get; set; }

        public decimal? Balance { get; set; }
    }
}
