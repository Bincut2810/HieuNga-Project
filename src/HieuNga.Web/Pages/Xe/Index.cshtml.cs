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

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public MotorcycleCategory? Category { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public bool FeaturedOnly { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        // Only return the grid partial for in-page filter requests targeting #catalog-grid.
        // Boosted navigations (e.g. homepage chips → /xe?category=0) also send HX-Request but
        // target #main-content and use hx-select="#main-content" — a bare partial has no
        // #main-content, so the listing would render blank.
        if (IsCatalogGridHtmxRequest())
            return Partial("_CatalogGrid", Result);

        this.SetSeo(null, "Danh sách xe máy Honda | Xe Máy Hiếu Nga",
            "Khám phá toàn bộ dòng xe Honda chính hãng tại Đà Nẵng.");
        return Page();
    }

    public async Task<IActionResult> OnGetFilterAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        return Partial("_CatalogGrid", Result);
    }

    private bool IsCatalogGridHtmxRequest()
    {
        if (!Request.Headers.ContainsKey("HX-Request"))
            return false;

        var target = Request.Headers["HX-Target"].ToString();
        return string.Equals(target, "catalog-grid", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        if (PageNumber < 1) PageNumber = 1;

        CategoryCounts = await motorcycleService.GetCategoryCountsAsync(ct);
        Result = await motorcycleService.SearchAsync(
            new MotorcycleFilterDto(Q, Category, MinPrice, MaxPrice, PageNumber, 12, FeaturedOnly ? true : null, Sort),
            ct);
    }
}
