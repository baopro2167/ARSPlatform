using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class NotificationUpdateRequest
    {
        public int? UserId { get; set; }

        public string? Message { get; set; }

        public bool? IsRead { get; set; }
    }
}
