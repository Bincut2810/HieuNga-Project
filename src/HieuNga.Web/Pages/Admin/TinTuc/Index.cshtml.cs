using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.TinTuc;

public class IndexModel(IBlogService blogService) : PageModel
{
    public Application.DTOs.PagedResultDto<Application.DTOs.BlogPostListItemDto> Posts { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Tin tức";
        Posts = await blogService.GetPublishedAsync(1, 50, null, ct);
    }
}
