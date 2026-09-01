using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class DetailedEvaluationUpdateRequest
    {
        public int? ReviewRequestId { get; set; }

        public int? ReviewerId { get; set; }

        public int? ScoreOriginality { get; set; }

        public string? NotesOriginality { get; set; }

        public int? ScoreLiterature { get; set; }

        public string? NotesLiterature { get; set; }

        public int? ScoreMethodology { get; set; }

        public string? NotesMethodology { get; set; }

        public int? ScoreResults { get; set; }

        public string? NotesResults { get; set; }

        public int? ScoreFormatting { get; set; }

        public string? NotesFormatting { get; set; }

        public string? GeneralComments { get; set; }

        public string? FinalDecision { get; set; }

        public List<SpecializedEvaluationItemRequest>? SpecializedEvaluation { get; set; }

        public string? ExpandedCriteria1 { get; set; }
        public string? Criteria1
        {
            get => ExpandedCriteria1;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria1 = value; }
        }
        public string? EvaluationCriteria1
        {
            get => ExpandedCriteria1;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria1 = value; }
        }

        public string? ExpandedCriteria2 { get; set; }
        public string? Criteria2
        {
            get => ExpandedCriteria2;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria2 = value; }
        }
        public string? EvaluationCriteria2
        {
            get => ExpandedCriteria2;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria2 = value; }
        }

        public string? ExpandedCriteria3 { get; set; }
        public string? Criteria3
        {
            get => ExpandedCriteria3;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria3 = value; }
        }
        public string? EvaluationCriteria3
        {
            get => ExpandedCriteria3;
            set { if (!string.IsNullOrEmpty(value)) ExpandedCriteria3 = value; }
        }
    }
}