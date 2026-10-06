using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarStatusUpdateRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = null!;
    }
}
