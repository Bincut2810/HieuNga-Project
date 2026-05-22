using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.KhuyenMai;

public class ChiTietModel(IPromotionService promotionService) : PageModel
{
    public Application.DTOs.PromotionDetailDto? Promotion { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Promotion = await promotionService.GetBySlugAsync(slug, ct);
        if (Promotion is null) return NotFound();

        this.SetSeo(Promotion.Seo, Promotion.Title, Promotion.Summary);
        return Page();
    }
}
