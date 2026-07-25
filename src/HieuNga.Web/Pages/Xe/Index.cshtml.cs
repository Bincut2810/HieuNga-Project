using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Enums;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Xe;

public class IndexModel(IMotorcycleService motorcycleService) : PageModel
{
    public PagedResultDto<MotorcycleListItemDto> Result { get; private set; } = null!;
    public IReadOnlyList<MotorcycleCategoryCountDto> CategoryCounts { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public MotorcycleCategory? Category { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        // In-page category/pagination swaps target #catalog-browse.
        // Boosted navigations (homepage → /xe?category=0) target #main-content and need the full page.
        if (IsCatalogBrowseHtmxRequest())
            return Partial("_CatalogBrowse", this);

        this.SetSeo(null, "Danh sách xe máy Honda | Xe Máy Hiếu Nga",
            "Khám phá toàn bộ dòng xe Honda chính hãng tại Đà Nẵng.");
        return Page();
    }

    private bool IsCatalogBrowseHtmxRequest()
    {
        if (!Request.Headers.ContainsKey("HX-Request"))
            return false;

        var target = Request.Headers["HX-Target"].ToString();
        return string.Equals(target, "catalog-browse", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        if (PageNumber < 1) PageNumber = 1;

        CategoryCounts = await motorcycleService.GetCategoryCountsAsync(ct);
        Result = await motorcycleService.SearchAsync(
            new MotorcycleFilterDto(Category, PageNumber, 12),
            ct);
    }
}
