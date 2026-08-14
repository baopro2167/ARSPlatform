using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class LearningMaterialUpdateRequest
    {
        public int? LecturerId { get; set; }

        public string Title { get; set; }

        public string? FileUrl { get; set; }

        public string? Description { get; set; }

        public int? SubFieldId { get; set; }
    }
}
