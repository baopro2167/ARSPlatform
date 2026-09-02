using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ResearchTopicUpdateRequest
    {
        public int TopicId { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? GuidanceProjectsUrl { get; set; }

        public int? LecturerId { get; set; }
    }
}
