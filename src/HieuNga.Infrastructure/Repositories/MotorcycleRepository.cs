using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Repositories;

public class MotorcycleRepository(HieuNgaDbContext context)
    : Repository<Motorcycle>(context), IMotorcycleRepository
{
    public async Task<Motorcycle?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await context.Motorcycles
            .Include(m => m.Variants)
            .Include(m => m.Colors)
            .Include(m => m.MediaAssets)
            .Include(m => m.Features)
            .Include(m => m.Technologies)
            .Include(m => m.SpinFrames)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsPublished, ct);

    public async Task<IReadOnlyList<Motorcycle>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        var featured = await context.Motorcycles
            .AsNoTracking()
            .Include(m => m.MediaAssets)
            .Include(m => m.Variants)
            .Where(m => m.IsFeatured && m.IsPublished && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .Take(count)
            .ToListAsync(ct);

        if (featured.Count > 0)
            return featured;

        // Fallback: latest published — keeps homepage conversion section alive
        return await context.Motorcycles
            .AsNoTracking()
            .Include(m => m.MediaAssets)
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Motorcycle> Items, int Total)> SearchAsync(
        string? query, MotorcycleCategory? category, decimal? minPrice, decimal? maxPrice,
        int page, int pageSize, CancellationToken ct = default,
        bool? featuredOnly = null, string? sort = null)
    {
        var q = context.Motorcycles.AsNoTracking()
            .Include(m => m.MediaAssets)
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(m => m.Name.Contains(query) || (m.ShortDescription != null && m.ShortDescription.Contains(query)));

        if (category.HasValue)
            q = q.Where(m => m.Category == category.Value);

        if (minPrice.HasValue)
            q = q.Where(m => m.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            q = q.Where(m => m.BasePrice <= maxPrice.Value);

        if (featuredOnly == true)
            q = q.Where(m => m.IsFeatured);

        var total = await q.CountAsync(ct);

        q = (sort ?? "default").ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(m => m.BasePrice).ThenBy(m => m.SortOrder),
            "price_desc" => q.OrderByDescending(m => m.BasePrice).ThenBy(m => m.SortOrder),
            "name" => q.OrderBy(m => m.Name),
            "newest" => q.OrderByDescending(m => m.CreatedAt),
            _ => q.OrderByDescending(m => m.IsFeatured).ThenBy(m => m.SortOrder).ThenByDescending(m => m.CreatedAt)
        };

        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<MotorcycleCategory, int>> GetPublishedCategoryCountsAsync(CancellationToken ct = default)
    {
        var rows = await context.Motorcycles.AsNoTracking()
            .Where(m => m.IsPublished && !m.IsDeleted)
            .GroupBy(m => m.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.Category, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<MotorcycleCategory, string?>> GetPublishedCategoryThumbnailsAsync(CancellationToken ct = default)
    {
        var rows = await context.Motorcycles.AsNoTracking()
            .Where(m => m.IsPublished && !m.IsDeleted)
            .OrderByDescending(m => m.IsFeatured)
            .ThenBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .Select(m => new { m.Category, m.ThumbnailUrl })
            .ToListAsync(ct);

        var map = new Dictionary<MotorcycleCategory, string?>();
        foreach (var row in rows)
        {
            if (map.ContainsKey(row.Category)) continue;
            map[row.Category] = row.ThumbnailUrl;
        }
        return map;
    }
}
