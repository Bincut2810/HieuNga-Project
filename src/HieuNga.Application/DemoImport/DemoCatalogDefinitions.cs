using HieuNga.Domain.Enums;

namespace HieuNga.Application.DemoImport;

/// <summary>In-memory demo dealership lineup sized to Sprint 3.6.1 inventory targets.</summary>
public static class DemoCatalogDefinitions
{
    public const string SharedAssetsFolder = "_Shared";

    public static IReadOnlyList<DemoMotorcycleMetadata> All { get; } = BuildAll();

    private static IReadOnlyList<DemoMotorcycleMetadata> BuildAll()
    {
        var list = new List<DemoMotorcycleMetadata>();
        var sort = 0;

        // ── Xe tay ga / Scooter (6) ──
        list.Add(Bike("Honda Vision", "demo-vision", "Scooter", 35_990_000, 110, "Tự động", true, sort++,
            "Xe tay ga đô thị gọn nhẹ, phù hợp đi lại hàng ngày."));
        list.Add(Bike("Honda Lead", "demo-lead", "Scooter", 39_490_000, 125, "Tự động", true, sort++,
            "Tay ga tiện nghi, cốp rộng, phù hợp gia đình trẻ."));
        list.Add(Bike("Honda Air Blade 160", "demo-air-blade-160", "Scooter", 57_690_000, 160, "Tự động", true, sort++,
            "Tay ga thể thao 160cc, thiết kế năng động."));
        list.Add(Bike("Honda SH160i", "demo-sh160i", "Scooter", 99_990_000, 160, "Tự động", true, sort++,
            "Xe tay ga cao cấp, phong cách châu Âu."));
        list.Add(Bike("Honda Vario 160", "demo-vario-160", "Scooter", 51_990_000, 160, "Tự động", false, sort++,
            "Tay ga cá tính, phù hợp giới trẻ đô thị."));
        list.Add(Bike("Honda PCX 160", "demo-pcx-160", "Scooter", 72_990_000, 160, "Tự động", true, sort++,
            "Tay ga trung cấp êm ái, phù hợp đi phố hàng ngày."));

        // ── Xe số (4) ──
        list.Add(Bike("Honda Wave Alpha", "demo-wave-alpha", "XeSo", 18_500_000, 110, "Số", true, sort++,
            "Xe số tiết kiệm, bền bỉ cho nhu cầu cơ bản."));
        list.Add(Bike("Honda Wave RSX", "demo-wave-rsx", "XeSo", 22_690_000, 110, "Số", false, sort++,
            "Xe số thể thao nhẹ, dễ vận hành."));
        list.Add(Bike("Honda Blade", "demo-blade", "XeSo", 21_390_000, 110, "Số", false, sort++,
            "Xe số phổ thông, chi phí sử dụng thấp."));
        list.Add(Bike("Honda Future 125", "demo-future-125", "XeSo", 31_500_000, 125, "Số", true, sort++,
            "Xe số 125cc êm ái, phù hợp đi đường dài."));

        // ── Xe côn tay (4) ──
        list.Add(Bike("Honda Winner X", "demo-winner-x", "ConTay", 46_160_000, 150, "Côn tay", true, sort++,
            "Underbone thể thao, phù hợp người mới chơi côn."));
        list.Add(Bike("Honda CB150 Verza", "demo-cb150-verza", "ConTay", 42_900_000, 150, "Côn tay", false, sort++,
            "Naked entry, dễ kiểm soát trong phố."));
        list.Add(Bike("Honda CBR150R", "demo-cbr150r", "ConTay", 72_500_000, 150, "Côn tay", true, sort++,
            "Sportbike 150cc đậm chất đường đua."));
        list.Add(Bike("Honda Sonic 150R", "demo-sonic-150r", "ConTay", 42_500_000, 150, "Côn tay", false, sort++,
            "Underbone cá tính, phù hợp giới trẻ."));

        // ── Xe phân khối lớn (4) ──
        list.Add(Bike("Honda CB500 Hornet", "demo-cb500-hornet", "PhanKhoiLon", 189_000_000, 500, "Côn tay", true, sort++,
            "Naked mid-size linh hoạt đường phố và tour ngắn."));
        list.Add(Bike("Honda CBR650R", "demo-cbr650r", "PhanKhoiLon", 268_000_000, 650, "Côn tay", true, sort++,
            "Sport mid-size cân bằng hiệu suất và tiện dụng."));
        list.Add(Bike("Honda CB650R", "demo-cb650r", "PhanKhoiLon", 246_000_000, 650, "Côn tay", false, sort++,
            "Neo Sports Café mạnh mẽ, phong cách hiện đại."));
        list.Add(Bike("Honda Rebel 500", "demo-rebel-500", "PhanKhoiLon", 187_000_000, 500, "Côn tay", false, sort++,
            "Cruiser cá tính, tư thế ngồi thoải mái."));

        // ── Xe điện (3) ──
        list.Add(Bike("Honda ICON e:", "demo-icon-e", "Electric", 21_990_000, null, "Tự động (điện)", true, sort++,
            "Xe điện đô thị nhỏ gọn — dữ liệu demo trình bày."));
        list.Add(Bike("Honda CUV e:", "demo-cuv-e", "Electric", 34_990_000, null, "Tự động (điện)", true, sort++,
            "Xe điện đa dụng, phù hợp đi phố."));
        list.Add(Bike("Honda EM1 e:", "demo-em1-e", "Electric", 29_990_000, null, "Tự động (điện)", false, sort++,
            "Xe điện cá nhân, vận hành êm."));

        return list;
    }

    private static DemoMotorcycleMetadata Bike(
        string name,
        string slug,
        string category,
        decimal price,
        int? engineCc,
        string transmission,
        bool featured,
        int sortOrder,
        string shortDesc)
    {
        var isElectric = string.Equals(category, "Electric", StringComparison.OrdinalIgnoreCase);
        var fuel = isElectric ? "Điện" : "Xăng";
        var consumption = isElectric ? "≈ 40–60 km/lần sạc (demo)" : engineCc switch
        {
            <= 110 => "≈ 1.5–2.0 L/100km (demo)",
            <= 160 => "≈ 2.0–2.6 L/100km (demo)",
            <= 250 => "≈ 2.5–3.5 L/100km (demo)",
            _ => "≈ 3.5–5.5 L/100km (demo)"
        };
        var warranty = isElectric ? "2 năm pin + 1 năm xe (demo)" : "3 năm hoặc 30.000 km (demo)";
        var thumb = CategoryThumbPath(category);

        return new DemoMotorcycleMetadata
        {
            Name = name,
            Slug = slug,
            Category = category,
            Price = price,
            Featured = featured,
            Published = true,
            SortOrder = sortOrder,
            ShortDescription = shortDesc,
            DescriptionHtml =
                $"<p><strong>{name}</strong> là mẫu xe demo tại Xe Máy Hiếu Nga phục vụ trình bày website và CMS. " +
                "Giá, thông số và mô tả có thể chỉnh sửa trong Admin.</p>",
            EngineCc = engineCc,
            FuelType = fuel,
            Transmission = transmission,
            Highlights =
            [
                "Dữ liệu demo — chỉnh sửa trong CMS",
                "Ảnh local ổn định (không phụ thuộc CDN)",
                "Máy tính trả góp bật mặc định"
            ],
            Specifications =
            [
                new DemoSpecItem { Icon = "⚡", Label = "Dung tích / động cơ", Value = engineCc.HasValue ? $"{engineCc} cc" : "Động cơ điện (demo)" },
                new DemoSpecItem { Icon = "⚙️", Label = "Hộp số", Value = transmission },
                new DemoSpecItem { Icon = "⛽", Label = "Nhiên liệu", Value = fuel },
                new DemoSpecItem { Icon = "📉", Label = "Mức tiêu hao", Value = consumption },
                new DemoSpecItem { Icon = "🛡️", Label = "Bảo hành", Value = warranty },
                new DemoSpecItem { Icon = "🏷️", Label = "Phân khúc", Value = CategoryLabel(category) },
                new DemoSpecItem { Icon = "📦", Label = "Tình trạng", Value = "Còn hàng (demo)" }
            ],
            Variants =
            [
                new DemoVariantItem
                {
                    Name = "Tiêu chuẩn",
                    Price = price,
                    StockQuantity = 5 + (sortOrder % 5),
                    IsAvailable = true,
                    Sku = slug.ToUpperInvariant()
                }
            ],
            Colors =
            [
                new DemoColorItem { Name = "Đen", Hex = "#1A1A1A" },
                new DemoColorItem { Name = "Trắng", Hex = "#F5F5F5" },
                new DemoColorItem { Name = "Đỏ", Hex = "#E40521" }
            ],
            Features =
            [
                new DemoContentCard
                {
                    Title = "Thiết kế nổi bật",
                    Description = $"Điểm nhấn thiết kế demo cho {name}. Thay nội dung trong CMS."
                },
                new DemoContentCard
                {
                    Title = "Vận hành hàng ngày",
                    Description = "Nội dung feature demo — editable trong tab Features."
                }
            ],
            Technology =
            [
                new DemoContentCard
                {
                    Title = "Công nghệ nổi bật",
                    Description = "Thông tin công nghệ demo — chỉnh trong CMS."
                }
            ],
            Seo = new DemoSeoMetadata
            {
                MetaTitle = $"{name} | Xe Máy Hiếu Nga",
                MetaDescription = shortDesc
            },
            Assets = new DemoAssetHints { Thumbnail = thumb }
        };
    }

    private static string CategoryThumbPath(string category) =>
        DemoPackageCatalog.ParseCategory(category) switch
        {
            MotorcycleCategory.Scooter => "/images/motorcycles/honda-vision-2025.svg",
            MotorcycleCategory.ConTay => "/images/motorcycles/honda-winner-x.svg",
            MotorcycleCategory.PhanKhoiLon => "/images/motorcycles/honda-cb150r.svg",
            MotorcycleCategory.XeSo => "/images/motorcycles/default.svg",
            MotorcycleCategory.Electric => "/images/motorcycles/default.svg",
            _ => "/images/motorcycles/default.svg"
        };

    private static string CategoryLabel(string category) =>
        DemoPackageCatalog.ParseCategory(category) switch
        {
            MotorcycleCategory.Scooter => "Xe tay ga",
            MotorcycleCategory.XeSo => "Xe số",
            MotorcycleCategory.ConTay => "Xe côn tay",
            MotorcycleCategory.PhanKhoiLon => "Xe phân khối lớn",
            MotorcycleCategory.Electric => "Xe điện",
            _ => category
        };
}
