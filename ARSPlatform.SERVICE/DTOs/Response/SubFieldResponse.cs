using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SubFieldResponse
    {
        public int SubFieldId { get; set; }

        public int? MajorFieldId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
