using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class GuidanceProjectUpdateRequest
    {
        public int? LecturerId { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public int? StudentId { get; set; }
    }
}
