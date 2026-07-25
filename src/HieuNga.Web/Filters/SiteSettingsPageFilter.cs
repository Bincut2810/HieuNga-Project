using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HieuNga.Web.Filters;

public class SiteSettingsPageFilter(ISiteSettingsService siteSettings) : IAsyncPageFilter
{
    public const string ViewDataKey = "SiteSettings";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerInstance is PageModel page)
        {
            var settings = await siteSettings.GetAsync(context.HttpContext.RequestAborted);
            page.ViewData[ViewDataKey] = settings;
            context.HttpContext.Items[ViewDataKey] = settings;
        }

        await next();
    }
}

public static class SiteSettingsViewData
{
    public static SiteSettingsDto Get(ViewDataDictionary viewData)
    {
        if (viewData[SiteSettingsPageFilter.ViewDataKey] is SiteSettingsDto s) return s;
        return new SiteSettingsDto(
            "Xe Máy Hiếu Nga", "0905 123 456", "0905 123 456",
            "https://zalo.me/0905123456", "contact@hondahieunga.vn",
            "123 Nguyễn Văn Linh, Đà Nẵng", "T2–T7: 8:00–18:00 · CN: 8:00–17:00",
            BrandDefaults.SeoTitle, BrandDefaults.SeoDescription,
            null, null,
            BrandDefaults.ServicePricingDisclaimer);
    }

    public static string TelHref(SiteSettingsDto s) =>
        "tel:" + new string(s.Hotline.Where(char.IsDigit).ToArray());

    public static string ZaloHref(SiteSettingsDto s)
    {
        if (!string.IsNullOrWhiteSpace(s.ZaloUrl)) return s.ZaloUrl.Trim();
        var digits = new string(s.Hotline.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? "https://zalo.me/" : $"https://zalo.me/{digits}";
    }

    public static string MapsSearchUrl(SiteSettingsDto s) =>
        "https://www.google.com/maps/search/?api=1&query=" +
        Uri.EscapeDataString(string.IsNullOrWhiteSpace(s.Address) ? s.SiteName : s.Address);
}
