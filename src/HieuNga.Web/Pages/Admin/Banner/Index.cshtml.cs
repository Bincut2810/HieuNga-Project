using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Banner;

public class IndexModel(IBannerRepository repository) : PageModel
{
    public IReadOnlyList<Application.DTOs.BannerDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Banner";
        var all = await repository.GetAllAsync(ct);
        Items = all.Where(b => !b.IsDeleted).OrderBy(b => b.SortOrder).Select(b => b.ToDto()).ToList();
    }
}
