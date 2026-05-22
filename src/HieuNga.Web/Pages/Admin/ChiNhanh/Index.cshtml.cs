using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.ChiNhanh;

public class IndexModel(IBranchService branchService) : PageModel
{
    public IReadOnlyList<Application.DTOs.BranchDto> Branches { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Chi nhánh";
        Branches = await branchService.GetActiveAsync(ct);
    }
}
