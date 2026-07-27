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
            .Include(m => m.Features)
            .Include(m => m.Technologies)
            .Include(m => m.SpinFrames)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsPublished && !m.IsDeleted, ct);

    public async Task<IReadOnlyList<Motorcycle>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        var featured = await context.Motorcycles
            .AsNoTracking()
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
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Motorcycle> Items, int Total)> SearchAsync(
        MotorcycleCategory? category,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = context.Motorcycles.AsNoTracking()
            .Include(m => m.Variants)
            .Where(m => m.IsPublished && !m.IsDeleted);

        if (category.HasValue)
            q = q.Where(m => m.Category == category.Value);

        var total = await q.CountAsync(ct);

        // Default showroom order: Featured → In stock → Newest → Name
        q = q
            .OrderByDescending(m => m.IsFeatured)
            .ThenByDescending(m =>
                !m.Variants.Any(v => !v.IsDeleted)
                || m.Variants.Any(v => !v.IsDeleted && v.IsAvailable))
            .ThenByDescending(m => m.CreatedAt)
            .ThenBy(m => m.Name);

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
