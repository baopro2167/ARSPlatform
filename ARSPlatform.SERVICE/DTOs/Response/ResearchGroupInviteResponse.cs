using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ResearchGroupInviteResponse
    {
        public int ResearchGroupId { get; set; }

        public int TotalInvited { get; set; }

        public List<string> SuccessEmails { get; set; } = new List<string>();

        public List<string> NotFoundEmails { get; set; } = new List<string>();

        public List<string> AlreadyMemberEmails { get; set; } = new List<string>();
    }
}
