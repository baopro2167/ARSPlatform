using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarFeedbackAiSummaryResponse
    {
        public int SeminarId { get; set; }
        public int FeedbackCount { get; set; }
        public SeminarFeedbackAiSummaryContentResponse Feedback { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class SeminarFeedbackAiSummaryContentResponse
    {
        public string OverallAssessment { get; set; } = string.Empty;
        public List<string> CommonStrengths { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
        public List<string> CommonSuggestions { get; set; } = new();
        public List<string> ConflictingFeedback { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }
}