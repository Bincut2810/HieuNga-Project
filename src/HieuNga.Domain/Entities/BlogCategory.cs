using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class BlogCategory : BaseEntity, ISeoEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }

    public ICollection<BlogPost> Posts { get; set; } = [];
}
