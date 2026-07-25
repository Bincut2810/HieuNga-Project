using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class ChiTietModel(IServiceCatalogService serviceCatalog) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var service = await serviceCatalog.GetBySlugAsync(slug, ct);
        if (service is null)
            return RedirectPermanent("/dich-vu");

        return RedirectPermanent($"/dich-vu/{service.Slug}");
    }
}
