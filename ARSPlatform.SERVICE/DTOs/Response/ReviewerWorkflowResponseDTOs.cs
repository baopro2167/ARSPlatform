using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ReviewCriterionItemResponse
    {
        public string CriterionCode { get; set; } = string.Empty;
        public string CriterionName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string RatingText { get; set; } = string.Empty;
        public string? Rationale { get; set; }
    }

    public class PaperReviewResponse
    {
        public int DetailedEvaluationId { get; set; }
        public int ReviewRequestId { get; set; }
        public int PaperId { get; set; }
        public string? PaperTitle { get; set; }
        public int? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? Recommendation { get; set; }
        public string? OverallSummary { get; set; }
        public string? Strengths { get; set; }
        public string? RequiredImprovements { get; set; }
        public string? RejectionReason { get; set; }
        public string? CommentsForResearcher { get; set; }
        
        /// <summary>
        /// Chỉ hiển thị cho Admin và Reviewer tạo review, ẩn với Tác giả / Public
        /// </summary>
        public string? PrivateCommentsForAdmin { get; set; }

        public bool? EthicsOrCopyrightConcern { get; set; }
        public string? ReviewedPaperVersion { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? AdminDecision { get; set; }
        public string? AdminDecisionNotes { get; set; }
        public DateTime? AdminDecisionAt { get; set; }
        public List<ReviewCriterionItemResponse> Criteria { get; set; } = new List<ReviewCriterionItemResponse>();
    }

    public class ReviewerAssignmentResponse
    {
        public int ReviewRequestId { get; set; }
        public int PaperId { get; set; }
        public string? PaperTitle { get; set; }
        public string? PaperAbstract { get; set; }
        public string? PaperFileUrl { get; set; }
        public int? SubFieldId { get; set; }
        public string? SubFieldName { get; set; }
        public int? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? Status { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? DeclinedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool? ConflictOfInterestDeclared { get; set; }
        public string? ConflictOfInterestReason { get; set; }
        public PaperReviewResponse? Review { get; set; }
    }

    public class AdminPaperReviewsSummaryResponse
    {
        public int PaperId { get; set; }
        public string PaperTitle { get; set; } = string.Empty;
        public string PaperStatus { get; set; } = string.Empty;
        public int? CreatorId { get; set; }
        public string? AuthorName { get; set; }
        public string? SubFieldName { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedReviews { get; set; }
        public int AcceptCount { get; set; }
        public int RevisionRequiredCount { get; set; }
        public int RejectCount { get; set; }
        public List<PaperReviewResponse> Reviews { get; set; } = new List<PaperReviewResponse>();
        public List<ReviewerAssignmentResponse> Assignments { get; set; } = new List<ReviewerAssignmentResponse>();
    }

    public class AuthorPaperReviewFeedbackResponse
    {
        public int PaperId { get; set; }
        public string PaperTitle { get; set; } = string.Empty;
        public string PaperStatus { get; set; } = string.Empty;
        public string? AdminDecisionNotes { get; set; }
        public DateTime? DecisionDate { get; set; }
        public bool IsFeedbackReleased { get; set; }
        public List<PaperReviewResponse> Reviews { get; set; } = new List<PaperReviewResponse>();
    }
}
