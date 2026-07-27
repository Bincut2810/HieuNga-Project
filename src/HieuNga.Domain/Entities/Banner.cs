using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class Banner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Carousel order (lower sorts first).</summary>
    public int SortOrder { get; set; }

    /// <summary>Published to the homepage.</summary>
    public bool IsActive { get; set; } = true;
}
