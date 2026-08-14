using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ReportUpdateRequest
    {
        public int? ReporterId { get; set; }

        public string? TargetType { get; set; }

        public int? TargetId { get; set; }

        public string Reason { get; set; }

        public string? Status { get; set; }

        public string? ViolationNotes { get; set; }
    }
}
