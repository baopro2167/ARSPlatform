using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class DetailedEvaluationResponse
    {
        public int DetailedEvaluationId { get; set; }

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

        public DateTime? CreatedAt { get; set; }

        public List<SpecializedEvaluationItemResponse> SpecializedEvaluation { get; set; }
            = new List<SpecializedEvaluationItemResponse>();

        public string? ExpandedCriteria1 { get; set; }
        public string? Criteria1 => ExpandedCriteria1;
        public string? EvaluationCriteria1 => ExpandedCriteria1;

        public string? ExpandedCriteria2 { get; set; }
        public string? Criteria2 => ExpandedCriteria2;
        public string? EvaluationCriteria2 => ExpandedCriteria2;

        public string? ExpandedCriteria3 { get; set; }
        public string? Criteria3 => ExpandedCriteria3;
        public string? EvaluationCriteria3 => ExpandedCriteria3;
    }
}