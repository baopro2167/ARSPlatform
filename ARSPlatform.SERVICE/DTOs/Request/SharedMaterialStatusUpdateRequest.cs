using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SharedMaterialStatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? RespondedAt { get; set; }
    }
}
