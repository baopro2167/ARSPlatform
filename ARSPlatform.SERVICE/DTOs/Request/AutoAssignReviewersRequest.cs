using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class AutoAssignReviewersRequest
    {
        [Required(ErrorMessage = "PaperId is required.")]
        public int PaperId { get; set; }

        [Range(1, 100, ErrorMessage = "ReviewerCount must be between 1 and 100.")]
        public int ReviewerCount { get; set; } = 3;
    }
}
