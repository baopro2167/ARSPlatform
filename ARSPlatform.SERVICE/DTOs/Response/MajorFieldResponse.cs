using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MajorFieldResponse
    {
        public int MajorFieldId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public IReadOnlyCollection<SubFieldResponse> SubFields { get; set; }
            = Array.Empty<SubFieldResponse>();
    }
}