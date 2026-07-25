using System.Text.Json;
using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Services;

/// <summary>Per-motorcycle finance UI prefs stored in SiteSetting (no schema change).</summary>
public sealed class MotorcycleFinancePrefs
{
    public bool CalculatorEnabled { get; set; } = true;
    public string? DefaultBankId { get; set; }
    public decimal DefaultDownPaymentPercent { get; set; } = 20;
    public int DefaultTermMonths { get; set; } = 12;

    public static string SettingKey(Guid motorcycleId) => $"motorcycle.finance.{motorcycleId:N}";

    public static async Task<MotorcycleFinancePrefs> LoadAsync(HieuNgaDbContext db, Guid motorcycleId, CancellationToken ct)
    {
        var key = SettingKey(motorcycleId);
        var row = await db.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.Value))
            return new MotorcycleFinancePrefs();
        try
        {
            return JsonSerializer.Deserialize<MotorcycleFinancePrefs>(row.Value) ?? new MotorcycleFinancePrefs();
        }
        catch
        {
            return new MotorcycleFinancePrefs();
        }
    }

    public static async Task SaveAsync(HieuNgaDbContext db, Guid motorcycleId, MotorcycleFinancePrefs prefs, CancellationToken ct)
    {
        var key = SettingKey(motorcycleId);
        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, ct);
        var json = JsonSerializer.Serialize(prefs);
        if (row is null)
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = json,
                Group = "motorcycle-finance"
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}
