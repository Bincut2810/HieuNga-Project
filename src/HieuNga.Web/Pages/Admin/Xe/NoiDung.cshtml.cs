using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.Xe;

public class NoiDungModel(
    IRepository<Motorcycle> motorcycleRepo,
    IRepository<MediaAsset> mediaRepo,
    IUnitOfWork uow,
    HieuNgaDbContext db) : PageModel
{
    public Guid MotorcycleId { get; set; }
    public string MotorcycleName { get; set; } = string.Empty;

    [BindProperty]
    public ContentInput Input { get; set; } = new();

    public class ContentInput
    {
        public string? MediaLines { get; set; }
        public string? HighlightsLines { get; set; }
        public string? SpecsLines { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Nội dung & hình ảnh";
        if (!await LoadAsync(id, ct)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Nội dung & hình ảnh";
        if (!await LoadAsync(id, ct)) return NotFound();

        var bike = await motorcycleRepo.GetByIdAsync(id, ct);
        if (bike is null || bike.IsDeleted) return NotFound();

        bike.HighlightsJson = SerializeHighlights(Input.HighlightsLines);
        bike.TechnicalSpecsJson = SerializeSpecs(Input.SpecsLines);

        var existing = await db.MediaAssets.Where(m => m.MotorcycleId == id && !m.IsDeleted).ToListAsync(ct);
        foreach (var asset in existing)
            await mediaRepo.SoftDeleteAsync(asset, ct);

        var order = 0;
        foreach (var line in ParseMediaLines(Input.MediaLines))
        {
            await mediaRepo.AddAsync(new MediaAsset
            {
                MotorcycleId = id,
                Url = line.Url,
                AltText = line.Alt,
                SortOrder = line.SortOrder >= 0 ? line.SortOrder : order++,
                FileName = Path.GetFileName(line.Url) ?? "image",
                Type = MediaType.Image
            }, ct);
        }

        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu nội dung.");
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        var bike = await db.Motorcycles.AsNoTracking()
            .Include(m => m.MediaAssets.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (bike is null) return false;

        MotorcycleId = bike.Id;
        MotorcycleName = bike.Name;
        Input = new ContentInput
        {
            MediaLines = string.Join(Environment.NewLine,
                bike.MediaAssets.OrderBy(a => a.SortOrder).Select(a => $"{a.Url}|{a.AltText}|{a.SortOrder}")),
            HighlightsLines = ParseHighlightsToLines(bike.HighlightsJson),
            SpecsLines = ParseSpecsToLines(bike.TechnicalSpecsJson)
        };
        return true;
    }

    private static string? ParseHighlightsToLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json);
            return items is null ? null : string.Join(Environment.NewLine, items);
        }
        catch { return json; }
    }

    private static string? ParseSpecsToLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var items = JsonSerializer.Deserialize<List<SpecJson>>(json);
            return items is null ? null : string.Join(Environment.NewLine,
                items.Select(s => $"{s.Label}|{s.Value}"));
        }
        catch { return json; }
    }

    private static string? SerializeHighlights(string? lines)
    {
        if (string.IsNullOrWhiteSpace(lines)) return null;
        var items = lines.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return items.Count == 0 ? null : JsonSerializer.Serialize(items);
    }

    private static string? SerializeSpecs(string? lines)
    {
        if (string.IsNullOrWhiteSpace(lines)) return null;
        var items = lines.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
                return new SpecJson { Icon = "•", Label = parts[0], Value = parts.Length > 1 ? parts[1] : "" };
            }).ToList();
        return items.Count == 0 ? null : JsonSerializer.Serialize(items);
    }

    private static IEnumerable<(string Url, string? Alt, int SortOrder)> ParseMediaLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var order = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            var url = parts[0];
            if (string.IsNullOrWhiteSpace(url)) continue;
            var alt = parts.Length > 1 ? parts[1] : null;
            var sort = parts.Length > 2 && int.TryParse(parts[2], out var s) ? s : order++;
            yield return (url, alt, sort);
        }
    }

    private sealed class SpecJson
    {
        public string? Icon { get; set; }
        public string? Label { get; set; }
        public string? Value { get; set; }
    }
}
