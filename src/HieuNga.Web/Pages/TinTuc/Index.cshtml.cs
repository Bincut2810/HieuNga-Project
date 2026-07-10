using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.TinTuc;

public class IndexModel(IBlogService blogService) : PageModel
{
    public BlogPostListItemDto? Featured { get; private set; }
    public PagedResultDto<BlogPostListItemDto> Posts { get; private set; } = null!;
    public IReadOnlyList<BlogCategoryDto> Categories { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public Guid? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await blogService.GetCategoriesAsync(ct);
        Featured = await blogService.GetFeaturedAsync(ct);
        Posts = await blogService.GetPublishedAsync(PageNumber, 9, CategoryId, ct);
        this.SetSeo(null, "Tin tức & Mẹo hay | Xe Máy Hiếu Nga", "Cập nhật tin tức xe máy Honda, mẹo bảo dưỡng và tư vấn mua xe.");
    }

    public async Task<IActionResult> OnGetFilterAsync(CancellationToken ct)
    {
        Posts = await blogService.GetPublishedAsync(PageNumber, 9, CategoryId, ct);
        return Partial("_BlogGrid", Posts);
    }
}
