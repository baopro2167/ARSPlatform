using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackFormRequest
    {
        /// <summary>
        /// Chuỗi JSON string hoặc object chứa danh sách câu hỏi
        /// </summary>
        [JsonPropertyName("feedback")]
        public object? Feedback { get; set; }

        /// <summary>
        /// Danh sách câu hỏi trực tiếp (nếu FE gửi questions thay vì feedback)
        /// </summary>
        [JsonPropertyName("questions")]
        public List<SeminarFeedbackQuestionDto>? Questions { get; set; }
    }
}
