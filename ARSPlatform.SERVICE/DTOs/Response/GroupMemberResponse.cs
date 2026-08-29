using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class GroupMemberResponse
    {
        public int GroupMemberId { get; set; }

        public int? ResearchGroupId { get; set; }

        public int? StudentId { get; set; }

        public string? ActivityStatus { get; set; }

        public bool? LeaderId { get; set; }

        public bool IsLeader { get; set; }

        public DateTime? JoinedAt { get; set; }

        public string? StudentName { get; set; }

        public string? StudentEmail { get; set; }

        public string? StudentAvatarUrl { get; set; }
    }
}
