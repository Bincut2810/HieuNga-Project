using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HieuNga.Application.Catalog;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Services;

public class ServiceCatalogService(HieuNgaDbContext db, ISiteSettingsService siteSettings) : IServiceCatalogService
{
    public async Task<string> PricingDisclaimerAsync(CancellationToken ct = default) =>
        (await siteSettings.GetAsync(ct)).ServicePricingDisclaimer;

    public string PricingDisclaimer => PricingDisclaimerAsync().GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ServiceItemListDto>> GetActiveItemsAsync(CancellationToken ct = default) =>
        await QueryActive()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new ServiceItemListDto(
                s.Slug, s.Name, s.Category.Name, s.IconKey ?? "wrench",
                s.EstimatedPriceText, s.EstimatedDurationText))
            .ToListAsync(ct);

    public async Task<ServiceItemDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var item = await QueryActive().FirstOrDefaultAsync(s => s.Slug == slug, ct);
        return item is null ? null : MapDetail(item);
    }

    public async Task<IReadOnlyList<ServiceItemListDto>> GetRelatedAsync(string slug, int count, CancellationToken ct)
    {
        var current = await db.ServiceItems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive && !s.IsDeleted, ct);
        if (current is null) return [];

        return await QueryActive()
            .Where(s => s.Slug != slug && s.ServiceCategoryId == current.ServiceCategoryId)
            .OrderBy(s => s.DisplayOrder)
            .Take(count)
            .Select(s => new ServiceItemListDto(
                s.Slug, s.Name, s.Category.Name, s.IconKey ?? "wrench",
                s.EstimatedPriceText, s.EstimatedDurationText))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetBookingServiceNamesAsync(CancellationToken ct = default) =>
        await QueryActive().OrderBy(s => s.DisplayOrder).Select(s => s.Name).ToListAsync(ct);

    private IQueryable<ServiceItem> QueryActive() =>
        db.ServiceItems.AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.IsActive && !s.IsDeleted && s.Category.IsActive && !s.Category.IsDeleted);

    private static ServiceItemDetailDto MapDetail(ServiceItem s) => new(
        s.Id, s.Slug, s.Name, s.Category.Name, s.IconKey ?? "wrench",
        s.ShortDescription ?? "", s.DetailDescription,
        ServiceItemJson.ParseIncludes(s.IncludesJson),
        s.EstimatedPriceText, s.EstimatedDurationText, s.PriceNote,
        new SeoMetadataDto(s.MetaTitle, s.MetaDescription, s.MetaKeywords, s.OgImageUrl, s.CanonicalUrl));
}

public class FinanceConfigService(HieuNgaDbContext db) : IFinanceConfigService
{
    public async Task<IReadOnlyList<FinanceBankDto>> GetActiveBanksAsync(CancellationToken ct = default)
    {
        var banks = await db.Banks.AsNoTracking()
            .Include(b => b.BankType)
            .Include(b => b.FinanceRates)
            .Where(b => b.IsActive && !b.IsDeleted && b.BankType.IsActive && !b.BankType.IsDeleted)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync(ct);

        return banks.Select(MapBank).Where(b => b is not null).Cast<FinanceBankDto>().ToList();
    }

    public async Task<FinanceBankDto?> GetDefaultBankAsync(CancellationToken ct = default)
    {
        var banks = await GetActiveBanksAsync(ct);
        return banks.FirstOrDefault(b => b.IsDefault) ?? banks.FirstOrDefault();
    }

    private static FinanceBankDto? MapBank(Bank bank)
    {
        var rate = bank.FinanceRates
            .Where(r => r.IsActive && !r.IsDeleted)
            .OrderByDescending(r => r.IsDefault)
            .ThenBy(r => r.DisplayOrder)
            .FirstOrDefault();
        if (rate is null) return null;

        var monthly = rate.MonthlyInterestRatePercent / 100m;
        return new FinanceBankDto(
            bank.ShortName.ToLowerInvariant(),
            bank.Name,
            bank.ShortName,
            monthly,
            rate.MonthlyInterestRatePercent,
            $"{rate.MonthlyInterestRatePercent.ToString("0.##", CultureInfo.InvariantCulture)}%/tháng",
            rate.TrustLabel,
            bank.BrandColor ?? "#E40521",
            rate.MinDownPaymentPercent,
            rate.MaxDownPaymentPercent,
            FinanceTerms.Parse(rate.SupportedTermsMonths, rate.MinTermMonths, rate.MaxTermMonths),
            rate.IsDefault);
    }
}

public class SiteSettingsService(HieuNgaDbContext db, IUnitOfWork uow) : ISiteSettingsService
{
    private static readonly Dictionary<string, string> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["site.name"] = BrandDefaults.SiteName,
        ["site.hotline"] = HieuNgaShowrooms.PrimaryPhone,
        ["site.phone"] = HieuNgaShowrooms.PrimaryPhone,
        ["site.zalo"] = "https://zalo.me/02363849556",
        ["site.email"] = "contact@hondahieunga.vn",
        ["site.address"] = HieuNgaShowrooms.PrimaryAddress,
        ["site.hours"] = HieuNgaShowrooms.OpeningHours,
        ["seo.default_title"] = BrandDefaults.SeoTitle,
        ["seo.default_description"] = BrandDefaults.SeoDescription,
        ["site.facebook"] = "",
        ["site.footer_text"] = "Đại lý xe máy uy tín tại Đà Nẵng",
        ["service.pricing_disclaimer"] = BrandDefaults.ServicePricingDisclaimer
    };

    public async Task<SiteSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var map = await db.SiteSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);

        string Get(string key) => map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v : Defaults.GetValueOrDefault(key, "");

        return new SiteSettingsDto(
            Get("site.name"), Get("site.hotline"), Get("site.phone"), Get("site.zalo"),
            Get("site.email"), Get("site.address"), Get("site.hours"),
            Get("seo.default_title"), Get("seo.default_description"),
            NullIfEmpty(Get("site.facebook")), NullIfEmpty(Get("site.footer_text")),
            Get("service.pricing_disclaimer"));
    }

    public async Task UpdateAsync(SiteSettingsDto settings, CancellationToken ct = default)
    {
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["site.name"] = settings.SiteName,
            ["site.hotline"] = settings.Hotline,
            ["site.phone"] = settings.Phone,
            ["site.zalo"] = settings.ZaloUrl,
            ["site.email"] = settings.Email,
            ["site.address"] = settings.Address,
            ["site.hours"] = settings.OpeningHours,
            ["seo.default_title"] = settings.DefaultMetaTitle,
            ["seo.default_description"] = settings.DefaultMetaDescription,
            ["site.facebook"] = settings.FacebookUrl ?? "",
            ["site.footer_text"] = settings.FooterText ?? "",
            ["service.pricing_disclaimer"] = settings.ServicePricingDisclaimer
        };

        var existing = await db.SiteSettings.ToListAsync(ct);
        foreach (var (key, value) in pairs)
        {
            var row = existing.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (row is null)
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = key.Split('.')[0] });
            else
            {
                row.Value = value;
                row.UpdatedAt = DateTime.UtcNow;
            }
        }

        await uow.SaveChangesAsync(ct);
    }

    private static string? NullIfEmpty(string v) => string.IsNullOrWhiteSpace(v) ? null : v;
}

public static class ServiceItemJson
{
    public static IReadOnlyList<string> ParseIncludes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return json.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); }
    }

    public static string SerializeIncludes(IEnumerable<string> items) =>
        JsonSerializer.Serialize(items.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList());
}

internal static class FinanceTerms
{
    public static IReadOnlyList<int> Parse(string? csv, int min, int max)
    {
        if (!string.IsNullOrWhiteSpace(csv))
        {
            var parsed = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).Distinct().OrderBy(n => n).ToList();
            if (parsed.Count > 0) return parsed;
        }
        return new List<int> { 6, 12, 18, 24, 36 }.Where(t => t >= min && t <= max).ToList();
    }
}

public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("N")[..8];
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c);
        }
        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant()
            .Replace('đ', 'd');
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
