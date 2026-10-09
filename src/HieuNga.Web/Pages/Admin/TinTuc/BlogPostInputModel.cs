using System.ComponentModel.DataAnnotations;

namespace HieuNga.Web.Pages.Admin.TinTuc;

/// <summary>
/// Staff input model for the Admin Blog CRUD (Create + Update).
///
/// Only fields the staff form actually renders are present here.
/// System-managed fields (Id, Slug, SEO, ViewCount, CreatedAt,
/// UpdatedAt, IsDeleted) are intentionally absent — the page model
/// owns them and must never let the browser bind to them.
///
/// Keeping this class small and free of the IAdminSeoInput surface
/// prevents accidental model binding from picking up legacy SEO
/// form fields that used to be posted in earlier iterations.
/// </summary>
public class BlogPostInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự.")]
    public string? Summary { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>URL of the uploaded cover image (set by the ImageUploader's hidden input).</summary>
    public string? ThumbnailUrl { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(120, ErrorMessage = "Tên tác giả tối đa 120 ký tự.")]
    public string? AuthorName { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool IsPublished { get; set; }
}
