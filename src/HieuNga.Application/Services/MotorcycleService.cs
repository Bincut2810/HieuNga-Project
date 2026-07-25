using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class MotorcycleService(IMotorcycleRepository repository) : IMotorcycleService
{
    public async Task<PagedResultDto<MotorcycleListItemDto>> SearchAsync(MotorcycleFilterDto filter, CancellationToken ct = default)
    {
        var (items, total) = await repository.SearchAsync(
            filter.Category, filter.Page, filter.PageSize, ct);

        return new PagedResultDto<MotorcycleListItemDto>(
            items.Select(m => m.ToListItem()).ToList(), total, filter.Page, filter.PageSize);
    }

    public async Task<MotorcycleDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var entity = await repository.GetBySlugAsync(slug, ct);
        return entity?.ToDetail();
    }

    public async Task<IReadOnlyList<MotorcycleListItemDto>> GetCompareListAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var all = await repository.FindAsync(m => idList.Contains(m.Id) && m.IsPublished && !m.IsDeleted, ct);
        return all.Select(m => m.ToListItem()).ToList();
    }

    public async Task<IReadOnlyList<MotorcycleCategoryCountDto>> GetCategoryCountsAsync(CancellationToken ct = default)
    {
        var counts = await repository.GetPublishedCategoryCountsAsync(ct);
        return MotorcycleCategoryLabels.All
            .Select(c => new MotorcycleCategoryCountDto(c.Value, c.Label, counts.GetValueOrDefault(c.Value)))
            .ToList();
    }

    public async Task<IReadOnlyList<MotorcycleListItemDto>> GetRelatedAsync(Guid motorcycleId, CancellationToken ct = default)
    {
        var current = await repository.GetByIdAsync(motorcycleId, ct);
        if (current is null || current.IsDeleted)
            return [];

        var price = current.BasePrice;
        var (sameCategory, _) = await repository.SearchAsync(
            current.Category, 1, 24, ct);

        var pool = sameCategory
            .Where(m => m.Id != motorcycleId)
            .Select(m => m.ToListItem())
            .ToList();

        // Fill with featured / similar-price bikes if same category is thin
        if (pool.Count < 6)
        {
            var (all, _) = await repository.SearchAsync(null, 1, 36, ct);
            var existing = pool.Select(p => p.Id).ToHashSet();
            foreach (var m in all.Where(x => x.Id != motorcycleId && !existing.Contains(x.Id)).Select(x => x.ToListItem()))
            {
                pool.Add(m);
                if (pool.Count >= 18) break;
            }
        }

        return pool
            .OrderByDescending(ScoreRelated)
            .ThenBy(m => Math.Abs(m.BasePrice - price))
            .Take(6)
            .ToList();

        double ScoreRelated(MotorcycleListItemDto m)
        {
            double score = 0;
            if (m.Category == current.Category) score += 120;
            if (m.IsAvailable) score += 100;
            if (m.IsFeatured) score += 60;
            if (price > 0)
            {
                var diffPct = (double)(Math.Abs(m.BasePrice - price) / price);
                score += Math.Max(0, 50 - diffPct * 120);
            }
            return score;
        }
    }
}
