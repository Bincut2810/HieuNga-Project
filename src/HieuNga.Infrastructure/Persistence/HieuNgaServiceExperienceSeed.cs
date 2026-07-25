using HieuNga.Application.Catalog;
using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Ensures the 6 flagship HEAD service experiences exist.
/// Fills empty media/content fields only; deactivates known legacy demo SKUs.
/// </summary>
public static class HieuNgaServiceExperienceSeed
{
    public static async Task EnsureAsync(HieuNgaDbContext context, ILogger logger, CancellationToken ct = default)
    {
        var category = await context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Slug == HieuNgaServiceExperience.CategorySlug && !c.IsDeleted, ct);

        if (category is null)
        {
            category = new ServiceCategory
            {
                Name = HieuNgaServiceExperience.CategoryName,
                Slug = HieuNgaServiceExperience.CategorySlug,
                Description = "Trải nghiệm dịch vụ HEAD tại Xe Máy Hiếu Nga",
                DisplayOrder = 0,
                IsActive = true
            };
            context.ServiceCategories.Add(category);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Seeded service category {Name}", category.Name);
        }
        else if (!category.IsActive)
        {
            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
        }

        var changed = false;
        foreach (var def in HieuNgaServiceExperience.All)
        {
            var item = await context.ServiceItems
                .FirstOrDefaultAsync(s => s.Slug == def.Slug && !s.IsDeleted, ct);

            if (item is null)
            {
                context.ServiceItems.Add(CreateFromDef(def, category.Id));
                changed = true;
                logger.LogInformation("Seeded experience service {Slug}", def.Slug);
                continue;
            }

            if (FillEmpty(item, def, category.Id))
            {
                changed = true;
                logger.LogInformation("Filled empty fields on experience service {Slug}", def.Slug);
            }
        }

        var legacySlugs = HieuNgaServiceExperience.LegacyDemoSlugs.ToList();
        foreach (var legacy in await context.ServiceItems
                     .Where(s => !s.IsDeleted && legacySlugs.Contains(s.Slug))
                     .ToListAsync(ct))
        {
            if (!legacy.IsActive) continue;
            legacy.IsActive = false;
            legacy.IsFeatured = false;
            legacy.UpdatedAt = DateTime.UtcNow;
            changed = true;
            logger.LogInformation("Deactivated legacy demo service {Slug}", legacy.Slug);
        }

        if (changed)
            await context.SaveChangesAsync(ct);
    }

    private static ServiceItem CreateFromDef(ServiceExperienceDef def, Guid categoryId) => new()
    {
        ServiceCategoryId = categoryId,
        Name = def.Name,
        Slug = def.Slug,
        ShortDescription = def.Summary,
        DetailDescription = def.Detail,
        IncludesJson = ServiceItemJson.SerializeIncludes(def.Benefits),
        WhenToUseJson = ServiceItemJson.SerializeIncludes(def.WhenToUse),
        ProcessJson = ServiceItemJson.SerializeIncludes(def.Process),
        FaqJson = ServiceItemJson.SerializeFaqs(def.Faqs.Select(f => new Application.DTOs.ServiceFaqDto(f.Q, f.A))),
        GalleryJson = ServiceItemJson.SerializeIncludes([def.ThumbnailUrl, def.HeroImageUrl]),
        ThumbnailUrl = def.ThumbnailUrl,
        HeroImageUrl = def.HeroImageUrl,
        OgImageUrl = def.HeroImageUrl,
        IconKey = def.IconKey,
        EstimatedPriceText = "Liên hệ tư vấn",
        EstimatedDurationText = "Theo hạng mục",
        PriceNote = "Báo giá rõ ràng sau khi kiểm tra",
        DisplayOrder = def.Order,
        IsFeatured = true,
        IsActive = true,
        MetaTitle = $"{def.Name} | Xe Máy Hiếu Nga",
        MetaDescription = def.Summary
    };

    private static bool FillEmpty(ServiceItem item, ServiceExperienceDef def, Guid categoryId)
    {
        var changed = false;

        if (item.ServiceCategoryId != categoryId) { item.ServiceCategoryId = categoryId; changed = true; }
        if (!item.IsFeatured) { item.IsFeatured = true; changed = true; }
        if (!item.IsActive) { item.IsActive = true; changed = true; }
        if (item.DisplayOrder != def.Order) { item.DisplayOrder = def.Order; changed = true; }

        if (string.IsNullOrWhiteSpace(item.Name)) { item.Name = def.Name; changed = true; }
        if (string.IsNullOrWhiteSpace(item.ShortDescription)) { item.ShortDescription = def.Summary; changed = true; }
        if (string.IsNullOrWhiteSpace(item.DetailDescription)) { item.DetailDescription = def.Detail; changed = true; }
        if (string.IsNullOrWhiteSpace(item.IncludesJson)) { item.IncludesJson = ServiceItemJson.SerializeIncludes(def.Benefits); changed = true; }
        if (string.IsNullOrWhiteSpace(item.WhenToUseJson)) { item.WhenToUseJson = ServiceItemJson.SerializeIncludes(def.WhenToUse); changed = true; }
        if (string.IsNullOrWhiteSpace(item.ProcessJson)) { item.ProcessJson = ServiceItemJson.SerializeIncludes(def.Process); changed = true; }
        if (string.IsNullOrWhiteSpace(item.FaqJson))
        {
            item.FaqJson = ServiceItemJson.SerializeFaqs(def.Faqs.Select(f => new Application.DTOs.ServiceFaqDto(f.Q, f.A)));
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(item.ThumbnailUrl)) { item.ThumbnailUrl = def.ThumbnailUrl; changed = true; }
        if (string.IsNullOrWhiteSpace(item.HeroImageUrl)) { item.HeroImageUrl = def.HeroImageUrl; changed = true; }
        if (string.IsNullOrWhiteSpace(item.GalleryJson))
        {
            item.GalleryJson = ServiceItemJson.SerializeIncludes([def.ThumbnailUrl, def.HeroImageUrl]);
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(item.IconKey)) { item.IconKey = def.IconKey; changed = true; }
        if (string.IsNullOrWhiteSpace(item.EstimatedPriceText)) { item.EstimatedPriceText = "Liên hệ tư vấn"; changed = true; }
        if (string.IsNullOrWhiteSpace(item.OgImageUrl)) { item.OgImageUrl = def.HeroImageUrl; changed = true; }

        if (changed) item.UpdatedAt = DateTime.UtcNow;
        return changed;
    }
}
