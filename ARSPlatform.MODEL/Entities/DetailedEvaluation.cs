using System;
using System.Collections.Generic;

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

    public virtual ReviewRequest? ReviewRequest { get; set; }

    public virtual User? Reviewer { get; set; }
}