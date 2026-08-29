using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ReportResponse
    {
        public int ReportId { get; set; }

        public int? ReporterId { get; set; }

        public string? TargetType { get; set; }

        public int? TargetId { get; set; }

        public string Reason { get; set; }

        public string? Status { get; set; }

        public string? ViolationNotes { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
