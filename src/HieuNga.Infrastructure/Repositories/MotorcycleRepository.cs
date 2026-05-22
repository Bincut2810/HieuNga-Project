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
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsPublished, ct);

    public async Task<IReadOnlyList<Motorcycle>> GetFeaturedAsync(int count, CancellationToken ct = default) =>
        await context.Motorcycles
            .AsNoTracking()
            .Include(m => m.MediaAssets)
            .Where(m => m.IsFeatured && m.IsPublished)
            .OrderBy(m => m.SortOrder)
            .Take(count)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Motorcycle> Items, int Total)> SearchAsync(
        string? query, MotorcycleCategory? category, decimal? minPrice, decimal? maxPrice,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = context.Motorcycles.AsNoTracking().Include(m => m.MediaAssets).Where(m => m.IsPublished);

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(m => m.Name.Contains(query) || (m.ShortDescription != null && m.ShortDescription.Contains(query)));

        if (category.HasValue)
            q = q.Where(m => m.Category == category.Value);

        if (minPrice.HasValue)
            q = q.Where(m => m.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            q = q.Where(m => m.BasePrice <= maxPrice.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(m => m.SortOrder).ThenByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }
}
