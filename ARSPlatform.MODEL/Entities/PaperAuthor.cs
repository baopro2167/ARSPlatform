using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class PaperAuthor
{
    public int PaperAuthorId { get; set; }

    public int PaperId { get; set; }

    public int AuthorOrder { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? RawAuthorName { get; set; }

    public string? OrcidId { get; set; }

    public string? OpenAlexAuthorId { get; set; }

    public bool? IsCorresponding { get; set; }

    public string Source { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public virtual Paper Paper { get; set; } = null!;
}
