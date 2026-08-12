using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarResponse
    {
        public int SeminarId { get; set; }

        public Guid? OrganizerId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? OnlineLink { get; set; }

        public int? MaxParticipants { get; set; }

        public bool IsReminderSent { get; set; }

        public string? Status { get; set; }

        // Existing AI feature
        public string? AiSummary { get; set; }

        // Existing feedback feature
        public string? Feedback { get; set; }

        public List<SeminarParticipantResponse> Participants { get; set; }
            = new();
    }
}