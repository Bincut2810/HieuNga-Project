using HieuNga.Domain;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.Xe;

public class IndexModel(IRepository<Motorcycle> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public MotorcycleCategory? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public IReadOnlyList<Row> Items { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PublishedCount { get; private set; }
    public int HiddenCount { get; private set; }
    public int CategoryTypeCount { get; private set; }
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Q) || Category.HasValue || !string.IsNullOrWhiteSpace(Status);

    public record Row(
        Guid Id,
        string Name,
        string Slug,
        MotorcycleCategory Category,
        decimal BasePrice,
        bool IsPublished,
        DateTime? UpdatedAt,
        string? ThumbnailUrl,
        int AngleCount,
        int ColorCount)
    {
        /// <summary>UI-only media completeness (thumbnail + color images + angles).</summary>
        public int MediaPercent
        {
            get
            {
                var score = 0;
                if (!string.IsNullOrWhiteSpace(ThumbnailUrl)) score += 40;
                if (ColorCount >= 1) score += 40;
                if (AngleCount >= 6) score += 20;
                else if (AngleCount >= 2) score += 10;
                return score;
            }
        }
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Xe máy";
        await LoadStatsAsync(ct);
        Items = await QueryRowsAsync(ct);
    }

    public async Task<IActionResult> OnPostTogglePublishAsync(
        Guid id,
        string? q,
        MotorcycleCategory? category,
        string? status,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        entity.IsPublished = !entity.IsPublished;
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess(entity.IsPublished ? "Đã hiển thị xe trên website." : "Đã ẩn xe khỏi website.");
        return RedirectToPage(new { q, category, status });
    }

    public SelectList CategoryOptions => new(
        MotorcycleCategoryLabels.All.Select(c => new { Value = (int)c.Value, Text = c.Label }),
        "Value", "Text", Category.HasValue ? (int)Category.Value : (int?)null);

    private async Task LoadStatsAsync(CancellationToken ct)
    {
        var all = await db.Motorcycles.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Select(m => new { m.IsPublished, m.Category })
            .ToListAsync(ct);

        TotalCount = all.Count;
        PublishedCount = all.Count(m => m.IsPublished);
        HiddenCount = all.Count(m => !m.IsPublished);
        CategoryTypeCount = all.Select(m => m.Category).Distinct().Count();
    }

    private async Task<IReadOnlyList<Row>> QueryRowsAsync(CancellationToken ct)
    {
        var q = db.Motorcycles.AsNoTracking().Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            q = q.Where(m => m.Name.Contains(term) || m.Slug.Contains(term));
        }

        if (Category.HasValue)
            q = q.Where(m => m.Category == Category.Value);

        if (Status == "active")
            q = q.Where(m => m.IsPublished);
        else if (Status == "inactive")
            q = q.Where(m => !m.IsPublished);

        return await q.OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new Row(
                m.Id,
                m.Name,
                m.Slug,
                m.Category,
                m.BasePrice,
                m.IsPublished,
                m.UpdatedAt,
                m.ThumbnailUrl,
                m.SpinFrames.Count(s => !s.IsDeleted),
                m.Colors.Count(c => !c.IsDeleted && c.ImageUrl != null && c.ImageUrl != "")))
            .ToListAsync(ct);
    }
}
