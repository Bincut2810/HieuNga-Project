using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.KhuyenMai;

public class IndexModel(IPromotionService promotionService) : PageModel
{
    public IReadOnlyList<Application.DTOs.PromotionDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Khuyến mãi";
        Items = await promotionService.GetActiveAsync(ct);
    }
}
