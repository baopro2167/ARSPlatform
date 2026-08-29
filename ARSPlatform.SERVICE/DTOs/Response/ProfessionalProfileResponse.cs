using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ProfessionalProfileResponse
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }

        public string? OrcidId { get; set; }
        public bool? IsOrcidVerified { get; set; }
        public DateTime? OrcidVerifiedAt { get; set; }

        public int? Hindex { get; set; }
        public int? TotalCitations { get; set; }
        public int? PublicationCount { get; set; }
        public string? SyncStatus { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? SubFieldId { get; set; }
        public string? SubFieldName { get; set; }
        public int? MajorFieldId { get; set; }
        public string? MajorFieldName { get; set; }
        public decimal? ReviewFee { get; set; }
        public bool? IsAvailable { get; set; }
    }
}