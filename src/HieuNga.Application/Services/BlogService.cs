using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class BlogService(IBlogRepository repository) : IBlogService
{
    public async Task<BlogPostListItemDto?> GetFeaturedAsync(CancellationToken ct = default)
    {
        var post = await repository.GetFeaturedAsync(ct);
        return post?.ToListItem();
    }

    public async Task<PagedResultDto<BlogPostListItemDto>> GetPublishedAsync(int page, int pageSize, Guid? categoryId = null, CancellationToken ct = default)
    {
        var items = await repository.GetPublishedAsync(page, pageSize, categoryId, ct);
        var total = await repository.GetPublishedCountAsync(categoryId, ct);
        return new PagedResultDto<BlogPostListItemDto>(items.Select(p => p.ToListItem()).ToList(), total, page, pageSize);
    }

    public async Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await repository.GetCategoriesAsync(ct);
        return cats.Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug)).ToList();
    }

    public async Task<BlogDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var post = await repository.GetBySlugAsync(slug, ct);
        return post?.ToDetail();
    }
}
