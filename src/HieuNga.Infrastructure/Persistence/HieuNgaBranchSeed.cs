using HieuNga.Application.Catalog;
using HieuNga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Ensures the two Hiếu Nga HEAD showrooms exist.
/// Never duplicates; never overwrites non-placeholder CMS data.
/// </summary>
public static class HieuNgaBranchSeed
{
    public static async Task EnsureAsync(HieuNgaDbContext context, ILogger logger, CancellationToken ct = default)
    {
        var branches = await context.Branches.Where(b => !b.IsDeleted).ToListAsync(ct);
        var changed = false;

        foreach (var def in HieuNgaShowrooms.All)
        {
            var match = FindMatch(branches, def);
            if (match is null)
            {
                var placeholder = branches.FirstOrDefault(b =>
                    HieuNgaShowrooms.LooksLikePlaceholderBranch(b.Name, b.Address, b.Phone, b.Hotline)
                    && !HieuNgaShowrooms.All.Any(d =>
                        string.Equals(d.Slug, b.Slug, StringComparison.OrdinalIgnoreCase)
                        || AddressEquals(b.Address, d.Address)));

                if (placeholder is not null && def.SortOrder == 0)
                {
                    ApplyDef(placeholder, def, overwriteAll: true);
                    changed = true;
                    logger.LogInformation("Replaced placeholder branch with {Name}", def.Name);
                    continue;
                }

                var entity = new Branch();
                ApplyDef(entity, def, overwriteAll: true);
                entity.IsActive = true;
                context.Branches.Add(entity);
                branches.Add(entity);
                changed = true;
                logger.LogInformation("Seeded branch {Name}", def.Name);
                continue;
            }

            if (FillEmptyOrPlaceholder(match, def))
            {
                changed = true;
                logger.LogInformation("Filled empty/placeholder fields on branch {Name}", match.Name);
            }
        }

        // Soft-disable leftover placeholders that were not converted
        foreach (var leftover in branches.Where(b =>
                     HieuNgaShowrooms.LooksLikePlaceholderBranch(b.Name, b.Address, b.Phone, b.Hotline)
                     && !HieuNgaShowrooms.All.Any(d => string.Equals(d.Slug, b.Slug, StringComparison.OrdinalIgnoreCase))))
        {
            leftover.IsActive = false;
            leftover.IsDeleted = true;
            leftover.UpdatedAt = DateTime.UtcNow;
            changed = true;
            logger.LogInformation("Retired leftover placeholder branch {Name}", leftover.Name);
        }

        changed |= await EnsureSiteContactSettingsAsync(context, logger, ct);

        if (changed)
            await context.SaveChangesAsync(ct);
    }

    private static Branch? FindMatch(List<Branch> branches, ShowroomDef def) =>
        branches.FirstOrDefault(b => string.Equals(b.Slug, def.Slug, StringComparison.OrdinalIgnoreCase))
        ?? branches.FirstOrDefault(b => AddressEquals(b.Address, def.Address))
        ?? branches.FirstOrDefault(b =>
            !string.IsNullOrWhiteSpace(b.Address)
            && b.Address.Contains(def.Address.Split(',')[0], StringComparison.OrdinalIgnoreCase));

    private static bool AddressEquals(string? a, string b) =>
        !string.IsNullOrWhiteSpace(a)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void ApplyDef(Branch entity, ShowroomDef def, bool overwriteAll)
    {
        entity.Slug = def.Slug;
        entity.Name = def.Name;
        entity.Address = def.Address;
        entity.District = def.District;
        entity.City = def.City;
        entity.Phone = def.Phone;
        entity.Hotline = def.Phone;
        entity.OpeningHours = def.OpeningHours;
        entity.MapEmbedUrl = def.MapEmbedUrl;
        entity.IsHeadOffice = def.IsHeadOffice;
        entity.SortOrder = def.SortOrder;
        entity.IsActive = true;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTime.UtcNow;
        if (overwriteAll && entity.Email is null)
            entity.Email = "contact@hondahieunga.vn";
    }

    /// <summary>Only fills blank or known-placeholder fields; preserves intentional CMS edits.</summary>
    private static bool FillEmptyOrPlaceholder(Branch entity, ShowroomDef def)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(entity.Slug)
            || entity.Slug.Contains("honda-hieu-nga", StringComparison.OrdinalIgnoreCase))
        {
            entity.Slug = def.Slug;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.Name)
            || entity.Name.Contains("Quận 7", StringComparison.OrdinalIgnoreCase)
            || entity.Name.Equals(BrandDefaults.LegacyBranchName, StringComparison.OrdinalIgnoreCase)
            || entity.Name.Equals(BrandDefaults.SiteNameWithCity, StringComparison.OrdinalIgnoreCase)
            || entity.Name.Equals(BrandDefaults.SiteName, StringComparison.OrdinalIgnoreCase))
        {
            entity.Name = def.Name;
            changed = true;
        }

        if (HieuNgaShowrooms.IsPlaceholderAddress(entity.Address))
        {
            entity.Address = def.Address;
            entity.District = def.District;
            entity.City = def.City;
            changed = true;
        }

        if (HieuNgaShowrooms.IsPlaceholderPhone(entity.Phone))
        {
            entity.Phone = def.Phone;
            changed = true;
        }

        if (HieuNgaShowrooms.IsPlaceholderPhone(entity.Hotline))
        {
            entity.Hotline = def.Phone;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.OpeningHours)
            || entity.OpeningHours.Contains("8:00", StringComparison.Ordinal))
        {
            // Replace legacy demo hours; keep custom CMS hours that don't look like the old seed.
            if (string.IsNullOrWhiteSpace(entity.OpeningHours)
                || entity.OpeningHours.Contains("T2", StringComparison.OrdinalIgnoreCase)
                || entity.OpeningHours.Contains("8:00–18:00", StringComparison.Ordinal)
                || entity.OpeningHours.Contains("8:00 - 18:00", StringComparison.Ordinal))
            {
                entity.OpeningHours = def.OpeningHours;
                changed = true;
            }
        }

        if (string.IsNullOrWhiteSpace(entity.MapEmbedUrl))
        {
            entity.MapEmbedUrl = def.MapEmbedUrl;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.District))
        {
            entity.District = def.District;
            changed = true;
        }

        entity.SortOrder = def.SortOrder;
        entity.IsHeadOffice = def.IsHeadOffice;
        entity.IsActive = true;
        if (changed) entity.UpdatedAt = DateTime.UtcNow;
        return changed;
    }

    private static async Task<bool> EnsureSiteContactSettingsAsync(
        HieuNgaDbContext context, ILogger logger, CancellationToken ct)
    {
        var desired = new Dictionary<string, (string Value, string Group)>(StringComparer.OrdinalIgnoreCase)
        {
            ["site.hotline"] = (HieuNgaShowrooms.PrimaryPhone, "contact"),
            ["site.phone"] = (HieuNgaShowrooms.PrimaryPhone, "contact"),
            ["site.address"] = (HieuNgaShowrooms.PrimaryAddress, "contact"),
            ["site.hours"] = (HieuNgaShowrooms.OpeningHours, "contact"),
            ["site.zalo"] = ("https://zalo.me/02363849556", "contact"),
        };

        var existing = await context.SiteSettings.ToListAsync(ct);
        var changed = false;

        foreach (var (key, (value, group)) in desired)
        {
            var row = existing.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                context.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = group });
                changed = true;
                logger.LogInformation("Seeded site setting {Key}", key);
                continue;
            }

            var isPlaceholder = key switch
            {
                "site.address" => HieuNgaShowrooms.IsPlaceholderAddress(row.Value),
                "site.hotline" or "site.phone" => HieuNgaShowrooms.IsPlaceholderPhone(row.Value),
                "site.zalo" => row.Value.Contains("0905123456", StringComparison.Ordinal),
                "site.hours" => string.IsNullOrWhiteSpace(row.Value)
                    || row.Value.Contains("8:00–18:00", StringComparison.Ordinal)
                    || row.Value.Contains("8:00 - 18:00", StringComparison.Ordinal)
                    || row.Value.Contains("T2–T7", StringComparison.OrdinalIgnoreCase)
                    || row.Value.Contains("T2-T7", StringComparison.OrdinalIgnoreCase),
                _ => string.IsNullOrWhiteSpace(row.Value)
            };

            if (isPlaceholder)
            {
                row.Value = value;
                row.UpdatedAt = DateTime.UtcNow;
                changed = true;
                logger.LogInformation("Replaced placeholder site setting {Key}", key);
            }
        }

        return changed;
    }
}
