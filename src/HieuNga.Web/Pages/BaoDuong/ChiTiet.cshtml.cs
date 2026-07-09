using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class ChiTietModel(IServiceCatalogService serviceCatalog) : PageModel
{
    public ServiceItemDetailDto? Service { get; private set; }
    public IReadOnlyList<ServiceItemListDto> RelatedServices { get; private set; } = [];
    public string PricingDisclaimer { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Service = await serviceCatalog.GetBySlugAsync(slug, ct);
        if (Service is null)
            return RedirectToPage("/BaoDuong/Index");

        RelatedServices = await serviceCatalog.GetRelatedAsync(slug, 3, ct);
        PricingDisclaimer = serviceCatalog.PricingDisclaimer;
        this.SetSeo(
            Service.Seo,
            $"{Service.Name} | Dịch vụ Honda Hiếu Nga",
            $"{Service.ShortDescription} Giá tham khảo: {Service.EstimatedPrice}.");

        return Page();
    }
}
