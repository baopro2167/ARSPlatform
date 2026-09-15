using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class TopicLearningMaterialCreateRequest
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        [Url]
        public string FileUrl { get; set; } = null!;

        public string? Description { get; set; }

        public int? SubFieldId { get; set; }
    }
}
