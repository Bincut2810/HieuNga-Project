using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

public class XoaModel(IRepository<Motorcycle> repository, IUnitOfWork uow) : PageModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Xóa xe";
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();
        Id = entity.Id;
        Name = entity.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        await repository.SoftDeleteAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess($"Đã xóa xe \"{entity.Name}\".");
        return RedirectToPage("./Index");
    }
}
