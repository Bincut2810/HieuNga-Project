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
            filter.Query, filter.Category, filter.MinPrice, filter.MaxPrice,
            filter.Page, filter.PageSize, ct,
            filter.FeaturedOnly, filter.Sort);

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
            .Where(c => c.Count > 0)
            .ToList();
    }

    public async Task<IReadOnlyList<MotorcycleListItemDto>> GetRelatedAsync(Guid motorcycleId, CancellationToken ct = default)
    {
        var current = await repository.GetByIdAsync(motorcycleId, ct);
        if (current is null || current.IsDeleted)
            return [];

        var (items, _) = await repository.SearchAsync(
            null, current.Category, null, null, 1, 24, ct);

        var price = current.BasePrice;
        return items
            .Where(m => m.Id != motorcycleId)
            .Select(m => m.ToListItem())
            .OrderByDescending(ScoreRelated)
            .ThenBy(m => Math.Abs(m.BasePrice - price))
            .Take(4)
            .ToList();

        double ScoreRelated(MotorcycleListItemDto m)
        {
            double score = 0;
            if (m.IsAvailable) score += 100;
            if (m.IsFeatured) score += 50;
            if (price > 0)
            {
                var diffPct = (double)(Math.Abs(m.BasePrice - price) / price);
                score += Math.Max(0, 40 - diffPct * 100);
            }
            return score;
        }
    }
}
