using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IMotorcycleService
{
    Task<PagedResultDto<MotorcycleListItemDto>> SearchAsync(MotorcycleFilterDto filter, CancellationToken ct = default);
    Task<MotorcycleDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<MotorcycleListItemDto>> GetCompareListAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
