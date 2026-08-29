using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ResearchGroupInviteRequest
    {
        public List<string> Emails { get; set; } = new List<string>();
    }
}
