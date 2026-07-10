using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages;

public class IndexModel(IHomepageService homepageService) : PageModel
{
    public HomepageDto Data { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Data = await homepageService.GetHomepageDataAsync(ct);
        this.SetSeo(null, "Xe Máy Hiếu Nga | Mua xe và dịch vụ xe máy",
            "Mua xe Honda chính hãng, tư vấn trả góp và đặt lịch sửa chữa, bảo dưỡng tại Xe Máy Hiếu Nga Đà Nẵng.");
    }
}
