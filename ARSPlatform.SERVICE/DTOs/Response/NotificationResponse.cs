using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class NotificationResponse
    {
        public int NotificationId { get; set; }

        public int? UserId { get; set; }

        public string? Message { get; set; }

        public bool? IsRead { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
