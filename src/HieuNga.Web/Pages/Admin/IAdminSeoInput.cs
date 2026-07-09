namespace HieuNga.Web.Pages.Admin;

public interface IAdminSeoInput
{
    string? MetaTitle { get; set; }
    string? MetaDescription { get; set; }
    string? MetaKeywords { get; set; }
    string? OgImageUrl { get; set; }
    string? CanonicalUrl { get; set; }
}
