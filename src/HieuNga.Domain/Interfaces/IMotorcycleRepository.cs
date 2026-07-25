using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Interfaces;

public interface IMotorcycleRepository : IRepository<Motorcycle>
{
    Task<Motorcycle?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Motorcycle>> GetFeaturedAsync(int count, CancellationToken ct = default);
    Task<(IReadOnlyList<Motorcycle> Items, int Total)> SearchAsync(
        string? query,
        MotorcycleCategory? category,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken ct = default,
        bool? featuredOnly = null,
        string? sort = null);

    Task<IReadOnlyDictionary<MotorcycleCategory, int>> GetPublishedCategoryCountsAsync(CancellationToken ct = default);
}
