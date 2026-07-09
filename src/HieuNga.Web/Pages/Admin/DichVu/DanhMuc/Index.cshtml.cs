using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.DichVu;

public class CategoryInputModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Slug { get; set; }

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DanhMucIndexModel(IRepository<ServiceCategory> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<ServiceCategory> Items { get; private set; } = [];

    [BindProperty]
    public CategoryInputModel Input { get; set; } = new();

    private async Task LoadItemsAsync(Guid? editId, CancellationToken ct)
    {
        Items = await db.ServiceCategories.AsNoTracking()
            .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync(ct);

        if (editId.HasValue)
        {
            var c = Items.FirstOrDefault(x => x.Id == editId.Value);
            if (c is not null)
                Input = new CategoryInputModel
                {
                    Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description,
                    DisplayOrder = c.DisplayOrder, IsActive = c.IsActive
                };
        }
    }

    public async Task OnGetAsync(Guid? editId, CancellationToken ct)
    {
        ViewData["Title"] = "Danh mục dịch vụ";
        await LoadItemsAsync(editId, ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Danh mục dịch vụ";
        if (!ModelState.IsValid)
        {
            await LoadItemsAsync(Input.Id, ct);
            return Page();
        }

        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (Input.Id.HasValue)
        {
            var entity = await repo.GetByIdAsync(Input.Id.Value, ct);
            if (entity is null || entity.IsDeleted) return NotFound();
            if (await db.ServiceCategories.AnyAsync(c => c.Slug == slug && c.Id != Input.Id && !c.IsDeleted, ct))
            {
                ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
                await LoadItemsAsync(Input.Id, ct);
                return Page();
            }
            entity.Name = Input.Name.Trim();
            entity.Slug = slug;
            entity.Description = Input.Description;
            entity.DisplayOrder = Input.DisplayOrder;
            entity.IsActive = Input.IsActive;
            await repo.UpdateAsync(entity, ct);
            this.SetSuccess("Đã cập nhật danh mục.");
        }
        else
        {
            if (await db.ServiceCategories.AnyAsync(c => c.Slug == slug && !c.IsDeleted, ct))
            {
                ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
                await LoadItemsAsync(null, ct);
                return Page();
            }
            await repo.AddAsync(new ServiceCategory
            {
                Name = Input.Name.Trim(),
                Slug = slug,
                Description = Input.Description,
                DisplayOrder = Input.DisplayOrder,
                IsActive = Input.IsActive
            }, ct);
            this.SetSuccess("Đã thêm danh mục.");
        }

        await uow.SaveChangesAsync(ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        await repo.SoftDeleteAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa danh mục.");
        return RedirectToPage();
    }
}
