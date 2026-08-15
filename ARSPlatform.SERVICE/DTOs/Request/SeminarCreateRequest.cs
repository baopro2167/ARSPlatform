using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarCreateRequest
    {
        public int? OrganizerId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Content { get; set; }

        public string? OnlineLink { get; set; }

        public int? MaxParticipants { get; set; }

        public bool? IsReminderSent { get; set; }

        public string? Status { get; set; }
    }
}