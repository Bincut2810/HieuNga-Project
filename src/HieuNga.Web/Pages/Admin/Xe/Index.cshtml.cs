using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

public class IndexModel(IMotorcycleRepository repository) : PageModel
{
    public IReadOnlyList<Application.DTOs.MotorcycleListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Quản lý xe";
        var all = await repository.GetAllAsync(ct);
        Items = all.Select(m => m.ToListItem()).ToList();
    }
}
