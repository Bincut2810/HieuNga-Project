using HieuNga.Domain.Entities;

namespace HieuNga.Domain.Interfaces;

public interface IBlogRepository
{
    Task<IReadOnlyList<BlogPost>> GetPublishedAsync(int page, int pageSize, Guid? categoryId = null, CancellationToken ct = default);
    Task<int> GetPublishedCountAsync(Guid? categoryId = null, CancellationToken ct = default);
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<BlogPost?> GetFeaturedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BlogCategory>> GetCategoriesAsync(CancellationToken ct = default);
}
