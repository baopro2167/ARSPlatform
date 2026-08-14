using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MajorFieldResponse
    {
        public int MajorFieldId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
