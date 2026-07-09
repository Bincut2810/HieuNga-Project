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
        this.SetSeo(null, "Honda Hiếu Nga Đà Nẵng | Mua xe & dịch vụ HEAD",
            "Mua xe Honda chính hãng, tư vấn trả góp và đặt lịch sửa chữa, bảo dưỡng tại HEAD Honda Hiếu Nga Đà Nẵng.");
    }
}
