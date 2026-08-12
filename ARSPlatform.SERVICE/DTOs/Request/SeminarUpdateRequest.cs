using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarUpdateRequest
    {
        public Guid? OrganizerId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? OnlineLink { get; set; }

        public int? MaxParticipants { get; set; }

        public string? Status { get; set; }
    }
}