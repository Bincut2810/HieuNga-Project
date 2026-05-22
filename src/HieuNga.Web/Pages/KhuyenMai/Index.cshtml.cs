using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.KhuyenMai;

public class IndexModel(IPromotionService promotionService) : PageModel
{
    public IReadOnlyList<Application.DTOs.PromotionDto> Promotions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Promotions = await promotionService.GetActiveAsync(ct);
        this.SetSeo(null, "Khuyến mãi xe máy Honda | Honda Hiếu Nga",
            "Ưu đãi, quà tặng, trả góp và sự kiện tại đại lý HEAD Đà Nẵng.");
    }
}
