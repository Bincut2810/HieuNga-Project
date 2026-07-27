using System.ComponentModel.DataAnnotations;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.DichVu;

public class BangGiaIndexModel(HieuNgaDbContext db, IRepository<ServiceItem> repo, IUnitOfWork uow) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public IReadOnlyList<Row> Items { get; private set; } = [];
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Status);

    public record Row(Guid Id, string Name, string Slug, string? ImageUrl, bool IsActive, int DisplayOrder);

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Dịch vụ";
        await LoadPageAsync(ct);
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, string? search, string? status, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        entity.IsActive = !entity.IsActive;
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess(entity.IsActive ? "Đã xuất bản dịch vụ." : "Đã ẩn dịch vụ.");
        return RedirectToPage(new { search, status });
    }

    private async Task LoadPageAsync(CancellationToken ct)
    {
        var q = db.ServiceItems.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            q = q.Where(s => EF.Functions.ILike(s.Name, $"%{term}%"));
        }

        if (Status == "active")
            q = q.Where(s => s.IsActive);
        else if (Status == "inactive")
            q = q.Where(s => !s.IsActive);

        var rows = await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync(ct);
        Items = rows.Select(s => new Row(
            s.Id,
            s.Name,
            s.Slug,
            ServiceGallery.Parse(s.GalleryJson).FirstOrDefault(),
            s.IsActive,
            s.DisplayOrder)).ToList();
    }
}

public class BangGiaThemModel(IRepository<ServiceItem> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty]
    public CreateInput Input { get; set; } = new();

    public class CreateInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }
    }

    public void OnGet() => ViewData["Title"] = "Thêm dịch vụ";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm dịch vụ";
        if (!ModelState.IsValid) return Page();

        var categoryId = await db.ServiceCategories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

        if (categoryId is null)
        {
            ModelState.AddModelError(string.Empty, "Chưa có danh mục dịch vụ. Vui lòng tạo danh mục trước.");
            return Page();
        }

        var name = Input.Name.Trim();
        var slug = SlugHelper.Generate(name);
        if (await db.ServiceItems.AnyAsync(s => s.Slug == slug && !s.IsDeleted, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var entity = new ServiceItem
        {
            ServiceCategoryId = categoryId.Value,
            Name = name,
            Slug = slug,
            ShortDescription = string.IsNullOrWhiteSpace(Input.ShortDescription) ? null : Input.ShortDescription.Trim(),
            DisplayOrder = 0,
            IsActive = false
        };

        await repo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã tạo dịch vụ — tải ảnh và xuất bản khi sẵn sàng.");
        return RedirectToPage("/Admin/DichVu/BangGia/Sua", new { id = entity.Id });
    }
}

public class BangGiaSuaModel(HieuNgaDbContext db, IImageStorageService imageStorage) : PageModel
{
    public Guid ServiceId { get; private set; }
    public bool SupportsImageUpload { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa dịch vụ";
        var exists = await db.ServiceItems.AsNoTracking().AnyAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (!exists) return NotFound();

        ServiceId = id;
        SupportsImageUpload = imageStorage.SupportsUpload;
        return Page();
    }
}
