using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class MotorcycleService(IMotorcycleRepository repository) : IMotorcycleService
{
    public async Task<PagedResultDto<MotorcycleListItemDto>> SearchAsync(MotorcycleFilterDto filter, CancellationToken ct = default)
    {
        var (items, total) = await repository.SearchAsync(
            filter.Query, filter.Category, filter.MinPrice, filter.MaxPrice,
            filter.Page, filter.PageSize, ct);

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
}
