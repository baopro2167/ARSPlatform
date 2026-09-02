using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ResearchGroupResponse
    {
        public int ResearchGroupId { get; set; }

        public int? LecturerId { get; set; }

        public int? TopicId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? MaterialsUrl { get; set; }

        public string? LecturerName { get; set; }

        public string? TopicTitle { get; set; }

        public int MemberCount { get; set; }

        public System.Collections.Generic.List<GroupMemberResponse>? Members { get; set; }
    }
}
