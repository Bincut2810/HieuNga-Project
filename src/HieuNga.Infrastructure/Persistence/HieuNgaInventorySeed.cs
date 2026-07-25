using System.Text.Json;
using HieuNga.Application.Catalog;
using HieuNga.Application.DemoImport;
using HieuNga.Application.Mappings;
using HieuNga.Domain;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Ensures published motorcycle inventory meets category targets.
/// Uses local static SVG thumbs only — no download / scraper / Cloudinary.
/// </summary>
public static class HieuNgaInventorySeed
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<InventorySeedReport> EnsureAsync(
        HieuNgaDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var before = await CountPublishedAsync(context, ct);
        var created = new List<string>();

        foreach (var (category, target) in HieuNgaInventoryTargets.Targets)
        {
            var current = before.GetValueOrDefault(category);
            if (current >= target) continue;

            var needed = target - current;
            var candidates = DemoCatalogDefinitions.All
                .Where(m => DemoPackageCatalog.ParseCategory(m.Category) == category)
                .OrderBy(m => m.SortOrder)
                .ToList();

            foreach (var meta in candidates)
            {
                if (needed <= 0) break;

                var exists = await context.Motorcycles
                    .AnyAsync(m => m.Slug == meta.Slug && !m.IsDeleted, ct);
                if (exists)
                {
                    // Ensure published if soft-present but unpublished
                    var row = await context.Motorcycles
                        .FirstAsync(m => m.Slug == meta.Slug && !m.IsDeleted, ct);
                    if (!row.IsPublished || row.Category != category)
                    {
                        row.IsPublished = true;
                        row.Category = category;
                        if (string.IsNullOrWhiteSpace(row.ThumbnailUrl))
                            row.ThumbnailUrl = ResolveThumb(meta.Slug, category);
                        row.UpdatedAt = DateTime.UtcNow;
                        created.Add($"{meta.Slug} (republished)");
                        needed--;
                    }
                    continue;
                }

                context.Motorcycles.Add(CreateBike(meta, category));
                created.Add(meta.Slug);
                needed--;
                logger.LogInformation("Inventory seed created {Slug} ({Category})", meta.Slug, category);
            }

            // Synthetic fillers if catalog definitions still short (should not happen with sized catalog)
            var filler = 1;
            while (needed > 0)
            {
                var slug = $"demo-{category.ToString().ToLowerInvariant()}-fill-{filler}";
                if (await context.Motorcycles.AnyAsync(m => m.Slug == slug && !m.IsDeleted, ct))
                {
                    filler++;
                    continue;
                }

                var label = category.ToDisplayName();
                context.Motorcycles.Add(new Motorcycle
                {
                    Name = $"Honda {label} Demo {filler}",
                    Slug = slug,
                    Category = category,
                    BasePrice = 25_000_000m + filler * 1_000_000m,
                    ShortDescription = $"Mẫu demo {label} — chỉnh sửa trong CMS.",
                    Description = $"<p>Xe demo bổ sung cho danh mục {label}.</p>",
                    ThumbnailUrl = HieuNgaInventoryTargets.CategoryThumb(category),
                    EngineCc = category == MotorcycleCategory.Electric ? null : 110,
                    FuelType = category == MotorcycleCategory.Electric ? "Điện" : "Xăng",
                    Transmission = category == MotorcycleCategory.XeSo ? "Số"
                        : category is MotorcycleCategory.ConTay or MotorcycleCategory.PhanKhoiLon ? "Côn tay"
                        : "Tự động",
                    IsPublished = true,
                    IsFeatured = false,
                    SortOrder = 900 + filler,
                    Variants =
                    [
                        new MotorcycleVariant
                        {
                            Name = "Tiêu chuẩn",
                            Price = 25_000_000m + filler * 1_000_000m,
                            StockQuantity = 3,
                            IsAvailable = true
                        }
                    ]
                });
                created.Add(slug);
                needed--;
                filler++;
            }
        }

        if (created.Count > 0)
            await context.SaveChangesAsync(ct);

        var after = await CountPublishedAsync(context, ct);
        var report = new InventorySeedReport(before, after, created);
        await WriteReportAsync(report, logger);
        logger.LogInformation(
            "Inventory ensure done. Created/republished={Count}. After counts: {Counts}",
            created.Count,
            string.Join(", ", after.Select(kv => $"{kv.Key.ToDisplayName()}={kv.Value}")));
        return report;
    }

    private static async Task WriteReportAsync(InventorySeedReport report, ILogger logger)
    {
        try
        {
            var roots = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs"))
            };
            var docs = roots.FirstOrDefault(Directory.Exists);
            if (docs is null) return;

            static string Line(IReadOnlyDictionary<MotorcycleCategory, int> map) =>
                string.Join("\n", MotorcycleCategoryLabels.All.Select(c =>
                    $"| {c.Label} | {map.GetValueOrDefault(c.Value)} | {HieuNgaInventoryTargets.Targets.GetValueOrDefault(c.Value)} |"));

            var md =
                "# Sprint 3.6.1 — Inventory Ensure Report\n\n" +
                $"Generated: {DateTime.UtcNow:u}\n\n" +
                "## Category counts before\n\n| Category | Published | Target |\n|----------|-----------|--------|\n" +
                Line(report.Before) + "\n\n" +
                "## Demo motorcycles created / republished\n\n" +
                (report.CreatedOrRepublished.Count == 0
                    ? "_None — targets already met._\n"
                    : string.Join("\n", report.CreatedOrRepublished.Select(s => $"- `{s}`")) + "\n") +
                "\n## Category counts after\n\n| Category | Published | Target |\n|----------|-----------|--------|\n" +
                Line(report.After) + "\n";

            await File.WriteAllTextAsync(Path.Combine(docs, "PHASE3_SPRINT_3_6_1_INVENTORY.md"), md);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not write inventory report markdown");
        }
    }

    private static Motorcycle CreateBike(DemoMotorcycleMetadata meta, MotorcycleCategory category)
    {
        var thumb = ResolveThumb(meta.Slug, category);
        return new Motorcycle
        {
            Name = meta.Name.StartsWith("Honda", StringComparison.OrdinalIgnoreCase)
                ? meta.Name
                : $"Honda {meta.Name}",
            Slug = meta.Slug,
            Category = category,
            BasePrice = meta.Price,
            ShortDescription = meta.ShortDescription,
            Description = meta.DescriptionHtml,
            ThumbnailUrl = thumb,
            EngineCc = meta.EngineCc,
            FuelType = meta.FuelType,
            Transmission = meta.Transmission,
            HighlightsJson = JsonSerializer.Serialize(meta.Highlights ?? [], JsonOptions),
            TechnicalSpecsJson = JsonSerializer.Serialize(
                (meta.Specifications ?? []).Select(s => new { icon = s.Icon, label = s.Label, value = s.Value }),
                JsonOptions),
            MetaTitle = meta.Seo.MetaTitle ?? $"{meta.Name} | Xe Máy Hiếu Nga",
            MetaDescription = meta.Seo.MetaDescription ?? meta.ShortDescription,
            IsPublished = true,
            IsFeatured = meta.Featured,
            SortOrder = meta.SortOrder,
            Variants = (meta.Variants.Count > 0
                    ? meta.Variants
                    : [new DemoVariantItem { Name = "Tiêu chuẩn", Price = meta.Price, StockQuantity = 5, IsAvailable = true }])
                .Select(v => new MotorcycleVariant
                {
                    Name = v.Name,
                    Price = v.Price ?? meta.Price,
                    StockQuantity = v.StockQuantity,
                    IsAvailable = v.IsAvailable,
                    Sku = v.Sku
                }).ToList(),
            Colors = meta.Colors.Select((c, i) => new MotorcycleColor
            {
                Name = c.Name,
                HexCode = c.Hex,
                ImageUrl = thumb,
                SortOrder = i
            }).ToList()
        };
    }

    private static string ResolveThumb(string slug, MotorcycleCategory category)
    {
        var catalog = MotorcycleImageCatalog.GetThumbnail(slug);
        if (!string.Equals(catalog, MotorcycleImageCatalog.Default, StringComparison.OrdinalIgnoreCase))
            return catalog;
        return HieuNgaInventoryTargets.CategoryThumb(category);
    }

    private static async Task<Dictionary<MotorcycleCategory, int>> CountPublishedAsync(
        HieuNgaDbContext context, CancellationToken ct)
    {
        var rows = await context.Motorcycles.AsNoTracking()
            .Where(m => m.IsPublished && !m.IsDeleted)
            .GroupBy(m => m.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dict = new Dictionary<MotorcycleCategory, int>();
        foreach (var cat in Enum.GetValues<MotorcycleCategory>())
            dict[cat] = rows.FirstOrDefault(r => r.Key == cat)?.Count ?? 0;
        return dict;
    }
}

public sealed record InventorySeedReport(
    IReadOnlyDictionary<MotorcycleCategory, int> Before,
    IReadOnlyDictionary<MotorcycleCategory, int> After,
    IReadOnlyList<string> CreatedOrRepublished);
