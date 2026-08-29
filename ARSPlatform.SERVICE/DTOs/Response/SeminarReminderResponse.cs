using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarReminderResponse
    {
        public int SeminarId { get; set; }
        public int Eligible { get; set; }
        public int Sent { get; set; }
        public int Skipped { get; set; }
        public List<string> FailedEmails { get; set; } = new();
    }
}