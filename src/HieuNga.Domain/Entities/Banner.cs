using System.ComponentModel.DataAnnotations.Schema;
using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class Banner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    [Column("CtaText")]
    public string? PrimaryButtonText { get; set; }

    [Column("CtaUrl")]
    public string? PrimaryButtonUrl { get; set; }

    /// <summary>Display priority (lower sorts first).</summary>
    public int SortOrder { get; set; }

    /// <summary>Enabled = published to the homepage.</summary>
    public bool IsActive { get; set; } = true;
}
