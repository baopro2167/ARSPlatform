using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SharedMaterialResponse
    {
        public int SharedMaterialId { get; set; }

        public int? LecturerId { get; set; }

        public int? PaperId { get; set; }

        public int? SharedWithColleagueId { get; set; }

        public DateTime? SharedAt { get; set; }

        public string? Status { get; set; }
    }
}
