using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.SoSanh;

public class IndexModel(IMotorcycleService motorcycleService, CompareSessionService compareSession) : PageModel
{
    public IReadOnlyList<MotorcycleListItemDto> Motorcycles { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        this.SetSeo(null, "So sánh xe máy Honda | Xe Máy Hiếu Nga", "So sánh thông số và giá các dòng xe Honda.");
    }

    public IActionResult OnGetAdd(Guid id)
    {
        compareSession.Add(id);
        return Partial("_CompareToast");
    }

    public async Task<IActionResult> OnGetRemoveAsync(Guid id, CancellationToken ct)
    {
        compareSession.Remove(id);
        await LoadAsync(ct);
        return Partial("_CompareTable", Motorcycles);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var ids = compareSession.GetIds();
        Motorcycles = ids.Count > 0
            ? await motorcycleService.GetCompareListAsync(ids, ct)
            : [];
    }
}
