using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ResearchTopicResponse
    {
        public int TopicId { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? MaterialsUrl { get; set; }
    }
}
