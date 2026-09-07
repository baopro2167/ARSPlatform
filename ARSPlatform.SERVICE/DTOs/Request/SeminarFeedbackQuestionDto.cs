using System.Text.Json.Serialization;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackQuestionDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "rating"; // "rating" or "text"

        [JsonPropertyName("questionText")]
        public string? QuestionText { get; set; }

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("maxStar")]
        public int? MaxStar { get; set; }
    }
}
