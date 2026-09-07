using System.Text.Json.Serialization;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackAnswerDto
    {
        [JsonPropertyName("questionId")]
        public string? QuestionId { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // "rating" or "text"

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
