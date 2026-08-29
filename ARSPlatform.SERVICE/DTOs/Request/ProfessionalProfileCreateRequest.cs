using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ProfessionalProfileCreateRequest
    {
        public int UserId { get; set; }

        public int? Hindex { get; set; }

        public int? TotalCitations { get; set; }

        public int? PublicationCount { get; set; }

        public string? SyncStatus { get; set; }

        public int? SubFieldId { get; set; }

        public decimal? ReviewFee { get; set; }
    }
}