using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IPromotionService
{
    Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken ct = default);
    Task<PromotionDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
