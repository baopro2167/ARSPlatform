using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ProfessionalProfileUpdateRequest
    {
        public int UserId { get; set; }

        public string? OrcidId { get; set; }

        public int? Hindex { get; set; }

        public int? TotalCitations { get; set; }

        public int? PublicationCount { get; set; }

        public string? SyncStatus { get; set; }
    }
}
