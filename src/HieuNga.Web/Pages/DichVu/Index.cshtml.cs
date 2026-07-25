using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.DichVu;

public class IndexModel(IServiceCatalogService serviceCatalog, IBranchService branchService) : PageModel
{
    public IReadOnlyList<ServiceItemListDto> Services { get; private set; } = [];
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public string PricingDisclaimer { get; private set; } = "";

    public async Task OnGetAsync(CancellationToken ct)
    {
        Services = await serviceCatalog.GetExperienceServicesAsync(12, ct);
        Branches = await branchService.GetActiveAsync(ct);
        PricingDisclaimer = serviceCatalog.PricingDisclaimer;
        this.SetSeo(
            null,
            "Dịch vụ HEAD | Xe Máy Hiếu Nga",
            "Bảo dưỡng, sửa chữa, nhớt chính hãng, bảo hiểm và chăm sóc xe tại HEAD Hiếu Nga Đà Nẵng.");
        ViewData["BranchBookUrl"] = "/bao-duong#booking";
        ViewData["BranchBookLabel"] = "Đặt lịch dịch vụ";
    }
}
