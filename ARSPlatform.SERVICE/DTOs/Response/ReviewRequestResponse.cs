using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ReviewRequestResponse
    {
        public int ReviewRequestId { get; set; }

        public int? PaperId { get; set; }

        public int? ReviewerId { get; set; }

        public decimal? Fee { get; set; }

        public string? Status { get; set; }

        public DateTime? Deadline { get; set; }

        public bool? Airecommended { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? Type { get; set; }
    }
}
