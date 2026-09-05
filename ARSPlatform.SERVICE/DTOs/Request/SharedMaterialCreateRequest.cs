using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SharedMaterialCreateRequest
    {
        public int? LecturerId { get; set; }

        public int? LearningMaterialId { get; set; }

        public int? PaperId { get; set; }

        public int? SharedWithColleagueId { get; set; }

        public DateTime? SharedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public string? Status { get; set; }
    }
}
