using HieuNga.Application.Catalog;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HieuNga.Web.Filters;

public class SiteSettingsPageFilter(ISiteSettingsService siteSettings, IBranchService branchService) : IAsyncPageFilter
{
    public const string ViewDataKey = "SiteSettings";
    public const string BranchesViewDataKey = "ActiveBranches";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerInstance is PageModel page)
        {
            var ct = context.HttpContext.RequestAborted;
            var settings = await siteSettings.GetAsync(ct);
            var branches = await branchService.GetActiveAsync(ct);
            page.ViewData[ViewDataKey] = settings;
            page.ViewData[BranchesViewDataKey] = branches;
            context.HttpContext.Items[ViewDataKey] = settings;
            context.HttpContext.Items[BranchesViewDataKey] = branches;
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
            BrandDefaults.SiteName,
            HieuNgaShowrooms.PrimaryPhone,
            HieuNgaShowrooms.PrimaryPhone,
            "https://zalo.me/118680124068083722",
            "contact@hondahieunga.vn",
            HieuNgaShowrooms.PrimaryAddress,
            HieuNgaShowrooms.OpeningHours,
            BrandDefaults.SeoTitle,
            BrandDefaults.SeoDescription,
            null, null,
            BrandDefaults.ServicePricingDisclaimer);
    }

    public static IReadOnlyList<BranchDto> GetBranches(ViewDataDictionary viewData)
    {
        if (viewData[SiteSettingsPageFilter.BranchesViewDataKey] is IReadOnlyList<BranchDto> branches)
            return branches;
        return [];
    }

    public static string TelHref(SiteSettingsDto s) =>
        HieuNgaShowrooms.TelHref(s.Hotline);

    public static string TelHref(BranchDto b) =>
        HieuNgaShowrooms.TelHref(DisplayPhone(b));

    public static string DisplayPhone(BranchDto b) =>
        !string.IsNullOrWhiteSpace(b.Hotline) ? b.Hotline!
        : !string.IsNullOrWhiteSpace(b.Phone) ? b.Phone!
        : HieuNgaShowrooms.PrimaryPhone;

    public static string MapsUrl(BranchDto b) =>
        HieuNgaShowrooms.ResolveMapsUrl(b.Slug, b.Address);

    public static string ZaloHref(SiteSettingsDto s)
    {
        if (!string.IsNullOrWhiteSpace(s.ZaloUrl)) return s.ZaloUrl.Trim();
        var digits = new string(s.Hotline.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? "https://zalo.me/" : $"https://zalo.me/{digits}";
    }

    public static string MapsSearchUrl(SiteSettingsDto s) =>
        HieuNgaShowrooms.ResolveMapsUrl(null, s.Address);
}
