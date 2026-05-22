using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class PromotionService(IPromotionRepository repository) : IPromotionService
{
    public async Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var items = await repository.GetActiveAsync(ct: ct);
        return items.Select(p => p.ToDto()).ToList();
    }

    public async Task<PromotionDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var p = await repository.GetBySlugAsync(slug, ct);
        return p?.ToDetail();
    }
}
