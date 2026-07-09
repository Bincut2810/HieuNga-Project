using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.ChiNhanh;

public class IndexModel(IBranchRepository repository) : PageModel
{
    public IReadOnlyList<Application.DTOs.BranchDto> Branches { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Chi nhánh";
        var all = await repository.GetAllAsync(ct);
        Branches = all.Where(b => !b.IsDeleted).Select(b => b.ToDto()).ToList();
    }
}
