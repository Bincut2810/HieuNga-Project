using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.TinTuc;

public class ChiTietModel(IBlogService blogService) : PageModel
{
    public Application.DTOs.BlogDetailDto? Post { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Post = await blogService.GetBySlugAsync(slug, ct);
        if (Post is null) return NotFound();

        this.SetSeo(Post.Seo, Post.Title, Post.Summary);
        return Page();
    }
}
