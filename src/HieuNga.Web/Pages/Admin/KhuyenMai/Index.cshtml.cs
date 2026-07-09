using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.KhuyenMai;

public class IndexModel(IPromotionRepository repository) : PageModel
{
    public IReadOnlyList<Application.DTOs.PromotionDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Khuyến mãi";
        var all = await repository.GetAllAsync(ct);
        Items = all.Where(p => !p.IsDeleted).OrderByDescending(p => p.EndDate).Select(p => p.ToDto()).ToList();
    }
}
