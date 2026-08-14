using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class GuidanceProjectResponse
    {
        public int GuidanceProjectId { get; set; }

        public int? LecturerId { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? StudentId { get; set; }
    }
}
