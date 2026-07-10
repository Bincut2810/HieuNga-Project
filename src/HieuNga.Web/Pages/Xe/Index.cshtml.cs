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
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public MotorcycleCategory? Category { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Result = await motorcycleService.SearchAsync(new MotorcycleFilterDto(Q, Category, null, null, PageNumber, 12), ct);
        this.SetSeo(null, "Danh sách xe máy Honda | Xe Máy Hiếu Nga",
            "Khám phá toàn bộ dòng xe Honda chính hãng tại Đà Nẵng.");
    }

    public async Task<IActionResult> OnGetFilterAsync(CancellationToken ct)
    {
        Result = await motorcycleService.SearchAsync(new MotorcycleFilterDto(Q, Category, null, null, PageNumber, 12), ct);
        return Partial("_CatalogGrid", Result);
    }
}
