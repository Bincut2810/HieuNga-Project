using System.Text.Json;
using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Web.Services;

/// <summary>Per-motorcycle finance UI prefs stored in SiteSetting (no schema change).</summary>
public sealed class MotorcycleFinancePrefs
{
    public bool CalculatorEnabled { get; set; } = true;
    public string? DefaultBankId { get; set; }
    public decimal DefaultDownPaymentPercent { get; set; } = 20;
    public int DefaultTermMonths { get; set; } = 12;

    /// <summary>Fallback monthly rate when CMS banks are unavailable (0.79%/month).</summary>
    public const decimal FallbackMonthlyRate = 0.0079m;
    public const string FallbackBankName = "Đối tác trả góp";

    public static string SettingKey(Guid motorcycleId) => $"motorcycle.finance.{motorcycleId:N}";

    public static MotorcycleFinancePrefs CreateDefaults() => new();

    /// <summary>
    /// Selling price for finance: first positive variant price, else BasePrice.
    /// Zero-priced variants do not shadow a positive BasePrice.
    /// </summary>
    public static decimal ResolveEffectivePrice(decimal basePrice, IEnumerable<decimal> variantPrices)
    {
        foreach (var p in variantPrices)
        {
            if (p > 0) return p;
        }
        return basePrice > 0 ? basePrice : 0m;
    }

    public static async Task<MotorcycleFinancePrefs> LoadAsync(HieuNgaDbContext db, Guid motorcycleId, CancellationToken ct)
    {
        var key = SettingKey(motorcycleId);
        var row = await db.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.Value))
            return CreateDefaults();
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var prefs = JsonSerializer.Deserialize<MotorcycleFinancePrefs>(row.Value, opts);
            return Normalize(prefs ?? CreateDefaults());
        }
        catch
        {
            return CreateDefaults();
        }
    }

    public static async Task SaveAsync(HieuNgaDbContext db, Guid motorcycleId, MotorcycleFinancePrefs prefs, CancellationToken ct)
    {
        var key = SettingKey(motorcycleId);
        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, ct);
        var json = JsonSerializer.Serialize(Normalize(prefs));
        if (row is null)
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = json,
                Group = "motorcycle-finance"
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Creates default prefs when missing. Does not overwrite an existing row
    /// (preserves Admin Finance tab choices), but normalizes invalid down/term on load.
    /// </summary>
    public static async Task EnsureDefaultsAsync(HieuNgaDbContext db, Guid motorcycleId, CancellationToken ct)
    {
        var key = SettingKey(motorcycleId);
        var exists = await db.SiteSettings.AsNoTracking()
            .AnyAsync(s => s.Key == key && !s.IsDeleted, ct);
        if (exists) return;
        await SaveAsync(db, motorcycleId, CreateDefaults(), ct);
    }

    /// <summary>
    /// Batch-ensure defaults for every published motorcycle with an effective selling price.
    /// Returns slugs that still lack prefs after the run (should be empty).
    /// </summary>
    public static async Task<IReadOnlyList<string>> EnsureDefaultsForPublishedAsync(
        HieuNgaDbContext db, ILogger? logger = null, CancellationToken ct = default)
    {
        var bikes = await db.Motorcycles.AsNoTracking()
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted)
            .Select(m => new
            {
                m.Id,
                m.Slug,
                m.Name,
                m.BasePrice,
                VariantPrices = m.Variants.Where(v => !v.IsDeleted).Select(v => v.Price).ToList()
            })
            .ToListAsync(ct);

        var eligible = bikes
            .Where(b => ResolveEffectivePrice(b.BasePrice, b.VariantPrices) > 0)
            .ToList();

        var keys = eligible.Select(b => SettingKey(b.Id)).ToList();
        var existing = (await db.SiteSettings.AsNoTracking()
                .Where(s => !s.IsDeleted && keys.Contains(s.Key))
                .Select(s => s.Key)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var payload = JsonSerializer.Serialize(CreateDefaults());
        var added = 0;
        foreach (var bike in eligible)
        {
            var key = SettingKey(bike.Id);
            if (existing.Contains(key)) continue;
            db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = payload,
                Group = "motorcycle-finance"
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger?.LogInformation("Ensured default finance prefs for {Count} published motorcycles", added);
        }

        // Re-check missing (eligible without prefs)
        existing = (await db.SiteSettings.AsNoTracking()
                .Where(s => !s.IsDeleted && keys.Contains(s.Key))
                .Select(s => s.Key)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return eligible
            .Where(b => !existing.Contains(SettingKey(b.Id)))
            .Select(b => b.Slug)
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// Lists published bikes that still need finance attention (no prefs row and/or no effective price).
    /// </summary>
    public static async Task<FinanceAuditReport> AuditPublishedAsync(HieuNgaDbContext db, CancellationToken ct = default)
    {
        var bikes = await db.Motorcycles.AsNoTracking()
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted)
            .OrderBy(m => m.Slug)
            .ToListAsync(ct);

        var keys = bikes.Select(b => SettingKey(b.Id)).ToList();
        var prefRows = await db.SiteSettings.AsNoTracking()
            .Where(s => !s.IsDeleted && keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);

        var missingPrefs = new List<string>();
        var missingPrice = new List<string>();
        var calculatorOff = new List<string>();

        foreach (var bike in bikes)
        {
            var prices = bike.Variants.Where(v => !v.IsDeleted).Select(v => v.Price);
            var effective = ResolveEffectivePrice(bike.BasePrice, prices);
            if (effective <= 0)
                missingPrice.Add(bike.Slug);

            var key = SettingKey(bike.Id);
            if (!prefRows.TryGetValue(key, out var json) || string.IsNullOrWhiteSpace(json))
            {
                missingPrefs.Add(bike.Slug);
                continue;
            }

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prefs = JsonSerializer.Deserialize<MotorcycleFinancePrefs>(json, opts);
                if (prefs is { CalculatorEnabled: false })
                    calculatorOff.Add(bike.Slug);
            }
            catch
            {
                missingPrefs.Add(bike.Slug);
            }
        }

        return new FinanceAuditReport(missingPrefs, missingPrice, calculatorOff, bikes.Count);
    }

    private static MotorcycleFinancePrefs Normalize(MotorcycleFinancePrefs prefs)
    {
        if (prefs.DefaultDownPaymentPercent <= 0 || prefs.DefaultDownPaymentPercent > 90)
            prefs.DefaultDownPaymentPercent = 20;
        if (prefs.DefaultTermMonths <= 0 || prefs.DefaultTermMonths > 60)
            prefs.DefaultTermMonths = 12;
        return prefs;
    }
}

public sealed record FinanceAuditReport(
    IReadOnlyList<string> MissingPrefsSlugs,
    IReadOnlyList<string> MissingPriceSlugs,
    IReadOnlyList<string> CalculatorDisabledSlugs,
    int PublishedCount);
