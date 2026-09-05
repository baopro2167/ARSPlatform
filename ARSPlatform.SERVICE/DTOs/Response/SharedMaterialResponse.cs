using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SharedMaterialResponse
    {
        public int SharedMaterialId { get; set; }
        public int Id => SharedMaterialId;

        public string Direction { get; set; } = "outbound";

        public int? LecturerId { get; set; }
        public string? LecturerName { get; set; }

        public int? SharedWithColleagueId { get; set; }
        public string? SharedWithName { get; set; }

        public int? LearningMaterialId { get; set; }
        public string? LearningMaterialTitle { get; set; }
        public string? LearningMaterialUrl { get; set; }
        public string? Title { get; set; }
        public string? FileUrl { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
        public int? PaperId { get; set; }

        public DateTime? SharedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public string? Status { get; set; }
        public string? EffectiveStatus { get; set; }

        public bool CanRevoke { get; set; }
        public bool CanRespond { get; set; }
        public int DaysRemaining { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
