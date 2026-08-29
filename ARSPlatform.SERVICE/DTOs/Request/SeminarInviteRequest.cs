using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarInviteRequest
    {
        [Required]
        public List<string> Emails { get; set; } = new();
    }
}