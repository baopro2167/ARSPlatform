using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarUpdateRequest
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string? Content { get; set; }

        public int? MaxParticipants { get; set; }

        // Backward compatibility for FE30 Remind Pending.
        // true triggers pending-feedback reminder sending.
        public bool? IsReminderSent { get; set; }

        public bool? ReminderEnabled { get; set; }

        public string? Status { get; set; }
        
        public int? SubFieldId { get; set; }
    }
}