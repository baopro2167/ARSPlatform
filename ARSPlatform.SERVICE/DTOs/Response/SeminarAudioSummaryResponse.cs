using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarAudioSummaryResponse
    {
        public int SeminarId { get; set; }
        public string AiSummary { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}
