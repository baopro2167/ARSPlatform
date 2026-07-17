using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class GroupMemberResponse
    {
        public int GroupMemberId { get; set; }

        public int? ResearchGroupId { get; set; }

        public int? StudentId { get; set; }

        public string? ActivityStatus { get; set; }

        public DateTime? JoinedAt { get; set; }
    }
}
