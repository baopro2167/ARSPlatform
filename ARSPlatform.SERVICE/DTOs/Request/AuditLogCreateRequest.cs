using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class AuditLogCreateRequest
    {
        public int AdminId { get; set; }

        public string AdminName { get; set; } = null!;

        public string Action { get; set; } = null!;

        public string Target { get; set; } = null!;

        public string? TargetId { get; set; }

        public string? Details { get; set; }
    }
}
