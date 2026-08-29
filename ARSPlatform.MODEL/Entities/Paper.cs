using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class Paper
{
    public int PaperId { get; set; }

    public int? CreatorId { get; set; }

    public string Title { get; set; } = null!;

    public string? Abstract { get; set; }

    public string? FileUrl { get; set; }

    public bool? Issn { get; set; }

    public bool? IsOpenAccess { get; set; }

    public string? Quartile { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? SubFieldId { get; set; }

    public string? OpenAlexWorkId { get; set; }

    public string? Doi { get; set; }

    public DateTime? PublicationDate { get; set; }

    public string? SourceName { get; set; }

    public string? IssnValue { get; set; }

    public string AuthorshipVerificationStatus { get; set; } = "NOT_CHECKED";

    public DateTime? AuthorshipVerifiedAt { get; set; }

    public string? AuthorshipVerificationReason { get; set; }

    public virtual User? Creator { get; set; }

    [JsonIgnore]
    public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();

    [JsonIgnore]
    public virtual ICollection<PaperAuthor> PaperAuthors { get; set; } = new List<PaperAuthor>();

    [JsonIgnore]
    public virtual ICollection<ReviewRequest> ReviewRequests { get; set; } = new List<ReviewRequest>();

    [JsonIgnore]
    public virtual ICollection<SharedMaterial> SharedMaterials { get; set; } = new List<SharedMaterial>();

    public virtual SubField? SubField { get; set; }
}