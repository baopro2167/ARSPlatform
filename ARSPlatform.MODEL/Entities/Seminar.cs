using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.MODEL.Entities
{
    public class Seminar
    {
        public int SeminarId { get; set; }
        public int? OrganizerId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Content { get; set; }
        public string? OnlineLink { get; set; }
        public int? MaxParticipants { get; set; }
        public bool IsReminderSent { get; set; }
        public string? Status { get; set; }
        public string? AiSummary { get; set; }
        public string? Feedback { get; set; }
    }
}
