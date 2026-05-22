using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IBlogService
{
    Task<BlogPostListItemDto?> GetFeaturedAsync(CancellationToken ct = default);
    Task<PagedResultDto<BlogPostListItemDto>> GetPublishedAsync(int page, int pageSize, Guid? categoryId = null, CancellationToken ct = default);
    Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<BlogDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
