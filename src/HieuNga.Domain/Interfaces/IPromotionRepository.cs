using HieuNga.Domain.Entities;

namespace HieuNga.Domain.Interfaces;

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<IReadOnlyList<Promotion>> GetActiveAsync(int? count = null, CancellationToken ct = default);
    Task<Promotion?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
