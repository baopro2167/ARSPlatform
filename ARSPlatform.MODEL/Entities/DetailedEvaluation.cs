using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class DetailedEvaluation
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

    public string SpecializedEvaluation { get; set; } = "[]";

    public string? Recommendation { get; set; }

    public string? OverallSummary { get; set; }

    public string? Strengths { get; set; }

    public string? RequiredImprovements { get; set; }

    public string? RejectionReason { get; set; }

    public string? CommentsForResearcher { get; set; }

    public string? PrivateCommentsForAdmin { get; set; }

    public bool? EthicsOrCopyrightConcern { get; set; }

    public string? ReviewedPaperVersion { get; set; }

    public string? AdminDecision { get; set; }

    public string? AdminDecisionNotes { get; set; }

    public DateTime? AdminDecisionAt { get; set; }

    [JsonIgnore]
    public virtual ReviewRequest? ReviewRequest { get; set; }

    [JsonIgnore]
    public virtual User? Reviewer { get; set; }
}