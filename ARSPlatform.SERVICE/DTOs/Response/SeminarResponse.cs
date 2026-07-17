using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarResponse
    {
        public int SeminarId { get; set; }

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
