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
        this.SetSeo(null, "Honda Hiếu Nga Đà Nẵng | Đại lý HEAD chính hãng",
            "Khám phá xe máy Honda chính hãng tại Đà Nẵng. Trả góp 0%, lái thử miễn phí, bảo dưỡng chuyên nghiệp.");
    }
}
