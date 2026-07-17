using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class WalletResponse
    {
        public int WalletId { get; set; }

        public int? UserId { get; set; }

        public decimal? Balance { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
