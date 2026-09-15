using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class AssignTopicLearningMaterialRequest
    {
        [Required]
        public int LearningMaterialId { get; set; }
    }
}
