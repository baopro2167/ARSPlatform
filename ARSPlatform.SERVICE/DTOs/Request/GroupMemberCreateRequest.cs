using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class GroupMemberCreateRequest
    {
        public int? ResearchGroupId { get; set; }

        public int? StudentId { get; set; }

        public string? ActivityStatus { get; set; }

        public DateTime? JoinedAt { get; set; }
    }
}
