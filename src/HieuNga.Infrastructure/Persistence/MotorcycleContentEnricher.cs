using System.Text.Json;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Applies canonical demo motorcycle content from MotorcycleContentCatalog.
/// Invoked only when DbInitializer enables it (Development or SeedOptions:RunContentEnricher).
/// </summary>
public static class MotorcycleContentEnricher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task EnrichAsync(HieuNgaDbContext context, ILogger logger, CancellationToken ct = default)
    {
        var motorcycles = await context.Motorcycles
            .Include(m => m.Colors)
            .Include(m => m.Variants)
            .ToListAsync(ct);

        if (motorcycles.Count == 0) return;

        logger.LogInformation("Enriching motorcycle content for {Count} models...", motorcycles.Count);

        foreach (var bike in motorcycles)
        {
            if (!MotorcycleImageCatalog.IsValidImageUrl(bike.ThumbnailUrl))
            {
                bike.ThumbnailUrl = MotorcycleImageCatalog.GetThumbnail(bike.Slug);
                bike.OgImageUrl = bike.ThumbnailUrl;
            }

            var profile = MotorcycleContentCatalog.GetBySlug(bike.Slug);
            if (profile is null) continue;

            bike.ShortDescription = profile.ShortDescription;
            bike.Description = profile.DescriptionHtml.Trim();
            bike.Transmission = profile.Transmission;
            bike.ThumbnailUrl = MotorcycleImageCatalog.GetThumbnail(bike.Slug);
            bike.OgImageUrl = bike.ThumbnailUrl;
            bike.HighlightsJson = JsonSerializer.Serialize(profile.Highlights, JsonOptions);
            bike.TechnicalSpecsJson = JsonSerializer.Serialize(
                profile.Specifications.Select(s => new { s.Icon, s.Label, s.Value }), JsonOptions);

            SyncColors(context, bike, profile.Colors);
            SyncVariants(context, bike, profile.Variants);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Motorcycle content enrichment completed.");
    }

    private static void SyncColors(HieuNgaDbContext context, Motorcycle bike, IReadOnlyList<MotorcycleColorSeed> colors)
    {
        if (bike.Colors.Any(c => !c.IsDeleted)) return;

        foreach (var c in colors)
        {
            var colorImage = MotorcycleImageCatalog.IsValidImageUrl(c.ImageUrl)
                ? c.ImageUrl
                : MotorcycleImageCatalog.GetThumbnail(bike.Slug);

            context.MotorcycleColors.Add(new MotorcycleColor
            {
                MotorcycleId = bike.Id,
                Name = c.Name,
                HexCode = c.Hex,
                ImageUrl = colorImage,
                SortOrder = c.Sort
            });
        }
    }

    private static void SyncVariants(HieuNgaDbContext context, Motorcycle bike, IReadOnlyList<MotorcycleVariantSeed> variants)
    {
        if (bike.Variants.Any(v => !v.IsDeleted)) return;

        foreach (var v in variants)
        {
            context.MotorcycleVariants.Add(new MotorcycleVariant
            {
                MotorcycleId = bike.Id,
                Name = v.Name,
                Slug = v.Name.ToLowerInvariant().Replace(' ', '-'),
                Price = v.Price,
                Sku = v.Sku,
                StockQuantity = v.Stock,
                IsAvailable = v.Stock > 0
            });
        }
    }
}
