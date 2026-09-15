using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ResearchTopicLearningMaterial
{
    public int ResearchTopicLearningMaterialId { get; set; }

    public int TopicId { get; set; }

    public int LearningMaterialId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ResearchTopic Topic { get; set; } = null!;

    public virtual LearningMaterial LearningMaterial { get; set; } = null!;
}
