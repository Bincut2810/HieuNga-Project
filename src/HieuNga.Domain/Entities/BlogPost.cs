using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class BlogPost : BaseEntity, ISeoEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? AuthorName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }

    public BlogCategory? Category { get; set; }
}
