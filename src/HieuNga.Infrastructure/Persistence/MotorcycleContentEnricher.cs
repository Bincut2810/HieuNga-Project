using System.Text.Json;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
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
            .Include(m => m.MediaAssets)
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

            SyncGallery(context, bike, MotorcycleImageCatalog.ResolveGallery(bike.Slug, profile.GalleryUrls));
            SyncColors(context, bike, profile.Colors);
            SyncVariants(context, bike, profile.Variants);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Motorcycle content enrichment completed.");
    }

    private static void SyncGallery(HieuNgaDbContext context, Motorcycle bike, IReadOnlyList<string> urls)
    {
        var validUrls = urls.Where(MotorcycleImageCatalog.IsValidImageUrl).ToList();
        if (validUrls.Count == 0)
            validUrls = MotorcycleImageCatalog.ResolveGallery(bike.Slug, []).ToList();

        var existing = bike.MediaAssets.Where(a => !a.IsDeleted).OrderBy(a => a.SortOrder).ToList();
        if (existing.Count == validUrls.Count && existing.Select(a => a.Url).SequenceEqual(validUrls))
            return;

        foreach (var asset in existing)
            asset.IsDeleted = true;

        for (var i = 0; i < validUrls.Count; i++)
        {
            context.MediaAssets.Add(new MediaAsset
            {
                MotorcycleId = bike.Id,
                FileName = $"{bike.Slug}-{i + 1}.jpg",
                Url = validUrls[i],
                AltText = $"{bike.Name} - góc {i + 1}",
                Type = MediaType.Image,
                SortOrder = i
            });
        }
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
