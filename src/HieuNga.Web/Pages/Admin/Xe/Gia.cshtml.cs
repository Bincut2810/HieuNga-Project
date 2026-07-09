using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.Xe;

public class GiaModel(
    IRepository<MotorcycleVariant> variantRepo,
    IUnitOfWork uow,
    HieuNgaDbContext db) : PageModel
{
    public Guid MotorcycleId { get; set; }
    public string MotorcycleName { get; set; } = string.Empty;
    public IReadOnlyList<VariantRow> Variants { get; private set; } = [];

    [BindProperty]
    public VariantInput Input { get; set; } = new();

    public record VariantRow(Guid Id, string Name, decimal Price, int StockQuantity, bool IsAvailable);

    public class VariantInput
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phiên bản")]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool IsAvailable { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? edit, CancellationToken ct)
    {
        ViewData["Title"] = "Quản lý giá";
        if (!await LoadAsync(id, ct)) return NotFound();
        if (edit.HasValue)
        {
            var v = Variants.FirstOrDefault(x => x.Id == edit.Value);
            if (v is not null)
                Input = new VariantInput { Id = v.Id, Name = v.Name, Price = v.Price, StockQuantity = v.StockQuantity, IsAvailable = v.IsAvailable };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        if (!ModelState.IsValid) return Page();

        if (Input.Id.HasValue)
        {
            var variant = await variantRepo.GetByIdAsync(Input.Id.Value, ct);
            if (variant is null || variant.IsDeleted || variant.MotorcycleId != id) return NotFound();
            variant.Name = Input.Name.Trim();
            variant.Price = Input.Price;
            variant.StockQuantity = Input.StockQuantity;
            variant.IsAvailable = Input.IsAvailable;
            await variantRepo.UpdateAsync(variant, ct);
        }
        else
        {
            await variantRepo.AddAsync(new MotorcycleVariant
            {
                MotorcycleId = id,
                Name = Input.Name.Trim(),
                Price = Input.Price,
                StockQuantity = Input.StockQuantity,
                IsAvailable = Input.IsAvailable
            }, ct);
        }

        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu phiên bản giá.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid variantId, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        var variant = await variantRepo.GetByIdAsync(variantId, ct);
        if (variant is null || variant.MotorcycleId != id) return NotFound();
        await variantRepo.SoftDeleteAsync(variant, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa phiên bản.");
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        var bike = await db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (bike is null) return false;
        MotorcycleId = bike.Id;
        MotorcycleName = bike.Name;
        Variants = await db.MotorcycleVariants.AsNoTracking()
            .Where(v => v.MotorcycleId == id && !v.IsDeleted)
            .OrderBy(v => v.Name)
            .Select(v => new VariantRow(v.Id, v.Name, v.Price, v.StockQuantity, v.IsAvailable))
            .ToListAsync(ct);
        return true;
    }
}
