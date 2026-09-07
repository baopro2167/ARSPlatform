using System.Collections.Generic;
using System.Text.Json.Serialization;
using ARSPlatform.SERVICE.DTOs.Request;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarFeedbackFormResponse
    {
        [JsonPropertyName("seminarId")]
        public int SeminarId { get; set; }

        /// <summary>
        /// Chuỗi JSON danh sách câu hỏi lưu trong CSDL
        /// </summary>
        [JsonPropertyName("feedback")]
        public string? Feedback { get; set; }

        /// <summary>
        /// Danh sách câu hỏi đã được parse để FE tiện binding
        /// </summary>
        [JsonPropertyName("questions")]
        public List<SeminarFeedbackQuestionDto> Questions { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Feedback form updated successfully.";
    }
}
