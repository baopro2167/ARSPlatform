using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarInviteResponse
    {
        public int SeminarId { get; set; }
        public int Requested { get; set; }
        public int Added { get; set; }
        public int Sent { get; set; }
        public int Skipped { get; set; }
        public List<string> FailedEmails { get; set; } = new();
    }
}