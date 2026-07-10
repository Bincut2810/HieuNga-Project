namespace HieuNga.Web.Pages.BaoDuong;

public record MaintenanceServiceItem(
    string Slug,
    string Name,
    string Category,
    string IconKey,
    string ShortDescription,
    string EstimatedPrice,
    string EstimatedDuration,
    string? PriceNote = null);

public static class MaintenanceServiceCatalog
{
    public const string PricingDisclaimer =
        "Xe Máy Hiếu Nga sẽ kiểm tra tình trạng xe, tư vấn hạng mục cần làm và báo giá rõ ràng trước khi thực hiện.";

    public static IReadOnlyList<MaintenanceServiceItem> All { get; } =
    [
        new("bao-duong-dinh-ky", "Bảo dưỡng định kỳ", "Bảo dưỡng", "wrench",
            "Kiểm tra tổng quát xe theo mốc km để xe vận hành ổn định.",
            "", "30 – 60 phút", null),
    ];
}
