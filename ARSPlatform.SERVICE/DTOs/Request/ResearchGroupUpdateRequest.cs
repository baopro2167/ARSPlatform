using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ResearchGroupUpdateRequest
    {
        public int? LecturerId { get; set; }

        public int? TopicId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? AssignedAt { get; set; }

        public string? MaterialsUrl { get; set; }
    }
}
