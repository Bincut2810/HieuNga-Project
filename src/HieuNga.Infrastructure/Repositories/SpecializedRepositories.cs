using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Repositories;

public class PromotionRepository(HieuNgaDbContext context) : Repository<Promotion>(context), IPromotionRepository
{
    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(int? count = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        IQueryable<Promotion> query = context.Promotions.AsNoTracking()
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.EndDate);

        if (count.HasValue)
            query = query.Take(count.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<Promotion?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await context.Promotions.AsNoTracking()
            .Include(p => p.Motorcycle)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, ct);
}

public class BranchRepository(HieuNgaDbContext context) : Repository<Branch>(context), IBranchRepository
{
    public async Task<IReadOnlyList<Branch>> GetActiveAsync(CancellationToken ct = default) =>
        await context.Branches.AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted).OrderBy(b => b.SortOrder).ToListAsync(ct);
}

public class BannerRepository(HieuNgaDbContext context) : Repository<Banner>(context), IBannerRepository
{
    public async Task<IReadOnlyList<Banner>> GetByPositionAsync(BannerPosition position, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.Banners.AsNoTracking()
            .Where(b => b.IsActive && b.Position == position
                && (b.StartDate == null || b.StartDate <= now)
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);
    }
}

public class BlogRepository(HieuNgaDbContext context) : IBlogRepository
{
    private IQueryable<BlogPost> PublishedQuery => context.BlogPosts.AsNoTracking()
        .Where(p => p.IsPublished && p.PublishedAt <= DateTime.UtcNow);

    public async Task<IReadOnlyList<BlogPost>> GetPublishedAsync(int page, int pageSize, Guid? categoryId = null, CancellationToken ct = default)
    {
        var q = PublishedQuery;
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);

        return await q.OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> GetPublishedCountAsync(Guid? categoryId = null, CancellationToken ct = default)
    {
        var q = PublishedQuery;
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);
        return await q.CountAsync(ct);
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await PublishedQuery.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<BlogPost?> GetFeaturedAsync(CancellationToken ct = default) =>
        await PublishedQuery.OrderByDescending(p => p.PublishedAt).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BlogCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        await context.BlogCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ToListAsync(ct);
}

public class ReviewRepository(HieuNgaDbContext context) : Repository<Review>(context), IReviewRepository
{
    public async Task<IReadOnlyList<Review>> GetFeaturedAsync(int count, CancellationToken ct = default) =>
        await context.Reviews.AsNoTracking()
            .Include(r => r.Motorcycle)
            .Where(r => r.IsApproved && r.IsFeatured)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Review>> GetByMotorcycleAsync(Guid motorcycleId, CancellationToken ct = default) =>
        await context.Reviews.AsNoTracking()
            .Where(r => r.MotorcycleId == motorcycleId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
