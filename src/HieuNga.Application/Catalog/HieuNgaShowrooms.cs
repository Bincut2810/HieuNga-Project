namespace HieuNga.Application.Catalog;

/// <summary>
/// Canonical Xe Máy Hiếu Nga showroom locations.
/// Used for empty/placeholder seeding only — live CMS values always win.
/// </summary>
public static class HieuNgaShowrooms
{
    public const string OpeningHours = "07:30–19:00";
    public const string PrimaryPhone = "0236 384 9556";
    public const string SecondaryPhone = "0236 384 9551";
    public const string PrimaryAddress = "392 Hoàng Diệu, Hải Châu, Đà Nẵng";
    public const string SecondaryAddress = "170 Hùng Vương, Hải Châu, Đà Nẵng";
    public const string PrimaryMapsUrl = "https://maps.google.com/?q=392+Hoàng+Diệu+Đà+Nẵng";
    public const string SecondaryMapsUrl = "https://maps.google.com/?q=170+Hùng+Vương+Đà+Nẵng";

    public static readonly ShowroomDef Branch1 = new(
        Slug: "head-hieu-nga-1",
        Name: "HEAD Hiếu Nga 1",
        Address: PrimaryAddress,
        District: "Hải Châu",
        City: "Đà Nẵng",
        Phone: PrimaryPhone,
        OpeningHours: OpeningHours,
        MapsUrl: PrimaryMapsUrl,
        MapEmbedUrl: "https://www.google.com/maps?q=392+Hoàng+Diệu,+Hải+Châu,+Đà+Nẵng&output=embed",
        IsHeadOffice: true,
        SortOrder: 0);

    public static readonly ShowroomDef Branch2 = new(
        Slug: "head-hieu-nga-2",
        Name: "HEAD Hiếu Nga 2",
        Address: SecondaryAddress,
        District: "Hải Châu",
        City: "Đà Nẵng",
        Phone: SecondaryPhone,
        OpeningHours: OpeningHours,
        MapsUrl: SecondaryMapsUrl,
        MapEmbedUrl: "https://www.google.com/maps?q=170+Hùng+Vương,+Hải+Châu,+Đà+Nẵng&output=embed",
        IsHeadOffice: false,
        SortOrder: 1);

    public static IReadOnlyList<ShowroomDef> All { get; } = [Branch1, Branch2];

    public static bool IsPlaceholderAddress(string? address) =>
        string.IsNullOrWhiteSpace(address)
        || address.Contains("Nguyễn Văn Linh", StringComparison.OrdinalIgnoreCase)
        || address.Contains("Nguyen Van Linh", StringComparison.OrdinalIgnoreCase);

    public static bool IsPlaceholderPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits is "0905123456" or "02361234567";
    }

    public static bool LooksLikePlaceholderBranch(string? name, string? address, string? phone, string? hotline) =>
        IsPlaceholderAddress(address)
        || IsPlaceholderPhone(phone)
        || IsPlaceholderPhone(hotline)
        || (name?.Contains("Quận 7", StringComparison.OrdinalIgnoreCase) == true);

    public static string ResolveMapsUrl(string? slug, string? address)
    {
        if (string.Equals(slug, Branch1.Slug, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(address) && address.Contains("392", StringComparison.Ordinal)
                && address.Contains("Hoàng Diệu", StringComparison.OrdinalIgnoreCase)))
            return PrimaryMapsUrl;

        if (string.Equals(slug, Branch2.Slug, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(address) && address.Contains("170", StringComparison.Ordinal)
                && address.Contains("Hùng Vương", StringComparison.OrdinalIgnoreCase)))
            return SecondaryMapsUrl;

        var query = string.IsNullOrWhiteSpace(address) ? "Xe Máy Hiếu Nga Đà Nẵng" : address;
        return "https://maps.google.com/?q=" + Uri.EscapeDataString(query);
    }

    public static string TelHref(string? phone)
    {
        var digits = new string((phone ?? PrimaryPhone).Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? "tel:" : "tel:" + digits;
    }
}

public sealed record ShowroomDef(
    string Slug,
    string Name,
    string Address,
    string District,
    string City,
    string Phone,
    string OpeningHours,
    string MapsUrl,
    string MapEmbedUrl,
    bool IsHeadOffice,
    int SortOrder);
