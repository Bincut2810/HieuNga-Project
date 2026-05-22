using HieuNga.Application.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Extensions;

public static class SeoExtensions
{
    public static void SetSeo(this PageModel page, SeoMetadataDto? seo, string fallbackTitle, string? fallbackDescription = null)
    {
        var viewData = page.ViewData;
        viewData["MetaTitle"] = seo?.Title ?? fallbackTitle;
        viewData["MetaDescription"] = seo?.Description ?? fallbackDescription;
        viewData["MetaKeywords"] = seo?.Keywords;
        viewData["OgImage"] = seo?.OgImageUrl ?? "/images/og-default.jpg";
        viewData["CanonicalUrl"] = seo?.CanonicalUrl;
    }
}
