using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

public class Banner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string? Badge { get; set; }
    /// <summary>0–100 overlay darkness; higher = stronger shade over photography.</summary>
    public int OverlayStrength { get; set; } = 65;
    public BannerTextAlignment TextAlignment { get; set; } = BannerTextAlignment.Left;
    public BannerPosition Position { get; set; }
    /// <summary>Display priority (lower sorts first).</summary>
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
