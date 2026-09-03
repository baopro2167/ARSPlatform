using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarCreateRequest
    {
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int? MaxParticipants { get; set; }

        // Backward-compatible with the current FE checkbox.
        // On create this means reminder scheduling is enabled.
        public bool? IsReminderSent { get; set; }

        public string? Status { get; set; }

        public List<string>? GuestEmails { get; set; }
        
        public int? SubFieldId { get; set; }
    }
}