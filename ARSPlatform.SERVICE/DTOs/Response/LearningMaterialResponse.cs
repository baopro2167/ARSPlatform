using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class LearningMaterialResponse
    {
        public int LearningMaterialId { get; set; }

        public int? LecturerId { get; set; }

        public string Title { get; set; }

        public string? FileUrl { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? SubFieldId { get; set; }
    }
}
