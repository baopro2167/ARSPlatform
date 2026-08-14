using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SubFieldUpdateRequest
    {
        public int? MajorFieldId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }
    }
}
