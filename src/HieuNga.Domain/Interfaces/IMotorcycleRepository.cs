using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Interfaces;

public interface IMotorcycleRepository : IRepository<Motorcycle>
{
    Task<Motorcycle?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Motorcycle>> GetFeaturedAsync(int count, CancellationToken ct = default);
    Task<(IReadOnlyList<Motorcycle> Items, int Total)> SearchAsync(
        MotorcycleCategory? category,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<MotorcycleCategory, int>> GetPublishedCategoryCountsAsync(CancellationToken ct = default);

    /// <summary>One representative thumbnail URL per published category (for homepage showcase).</summary>
    Task<IReadOnlyDictionary<MotorcycleCategory, string?>> GetPublishedCategoryThumbnailsAsync(CancellationToken ct = default);
}
