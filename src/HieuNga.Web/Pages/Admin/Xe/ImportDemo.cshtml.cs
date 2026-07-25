using HieuNga.Application.DemoImport;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

public class ImportDemoModel(IDemoMotorcycleImporter importer) : PageModel
{
    public IReadOnlyList<DemoPackageInfo> Packages { get; private set; } = [];
    public IReadOnlyDictionary<string, int> CategoryCounts { get; private set; } =
        new Dictionary<string, int>();
    public int CatalogDefinitionCount => DemoCatalogDefinitions.All.Count;
    public string AssetsRoot => importer.AssetsRootPath;
    public bool StorageReady => importer.StorageReady;
    public string StorageDescription => importer.StorageDescription;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Packages = await importer.ListPackagesAsync(ct);
        CategoryCounts = await importer.GetPublishedCategoryCountsAsync(ct);
    }

    public async Task<IActionResult> OnPostImportAsync(string packageId, CancellationToken ct)
    {
        var result = await importer.ImportAsync(packageId, ct);
        if (!result.Success)
        {
            this.SetError(result.Message);
            return RedirectToPage();
        }

        var warn = result.Warnings.Count > 0
            ? " Cảnh báo: " + string.Join("; ", result.Warnings.Take(3))
            : "";
        this.SetSuccess($"{result.Message} ({result.UploadedImages} ảnh).{warn}");

        if (result.MotorcycleId is Guid id)
            return Redirect($"/admin/xe/editor/{id}");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSeedCatalogAsync(CancellationToken ct)
    {
        var result = await importer.SeedFullCatalogAsync(ct);
        if (!result.Success)
        {
            this.SetError(result.Message);
            return RedirectToPage();
        }

        var counts = string.Join(", ", result.CountsByCategory.Select(kv => $"{kv.Key}: {kv.Value}"));
        var warn = result.Warnings.Count > 0
            ? " Cảnh báo: " + string.Join("; ", result.Warnings.Take(5))
            : "";
        this.SetSuccess($"{result.Message} Phân bố — {counts}.{warn}");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string packageId, CancellationToken ct)
    {
        var result = await importer.DeleteDemoAsync(packageId, ct);
        if (!result.Success)
            this.SetError(result.Message);
        else
            this.SetSuccess(result.Message);
        return RedirectToPage();
    }
}
