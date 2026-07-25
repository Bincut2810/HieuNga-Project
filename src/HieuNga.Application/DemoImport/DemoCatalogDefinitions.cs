using HieuNga.Domain.Enums;

namespace HieuNga.Application.DemoImport;

/// <summary>In-memory demo dealership lineup (presentation seed). Editable later via CMS.</summary>
public static class DemoCatalogDefinitions
{
    public const string SharedAssetsFolder = "_Shared";

    public static IReadOnlyList<DemoMotorcycleMetadata> All { get; } = BuildAll();

    private static IReadOnlyList<DemoMotorcycleMetadata> BuildAll()
    {
        var list = new List<DemoMotorcycleMetadata>();
        var sort = 0;

        // ── Scooter (5) ──
        list.Add(Bike("Vision", "demo-vision", "Scooter", 35_990_000, 110, "Tự động", true, sort++,
            "Xe tay ga đô thị gọn nhẹ, phù hợp đi lại hàng ngày."));
        list.Add(Bike("Lead", "demo-lead", "Scooter", 39_490_000, 125, "Tự động", true, sort++,
            "Scooter tiện nghi, cốp rộng, phù hợp gia đình trẻ."));
        list.Add(Bike("Air Blade 160", "demo-air-blade-160", "Scooter", 57_690_000, 160, "Tự động", true, sort++,
            "Tay ga thể thao 160cc, thiết kế năng động."));
        list.Add(Bike("SH160i", "demo-sh160i", "Scooter", 99_990_000, 160, "Tự động", true, sort++,
            "Scooter cao cấp, phong cách châu Âu."));
        list.Add(Bike("Vario 160", "demo-vario-160", "Scooter", 51_990_000, 160, "Tự động", false, sort++,
            "Tay ga cá tính, phù hợp giới trẻ đô thị."));

        // ── Xe số (5) ──
        list.Add(Bike("Wave Alpha", "demo-wave-alpha", "XeSo", 18_500_000, 110, "Số", true, sort++,
            "Xe số tiết kiệm, bền bỉ cho nhu cầu cơ bản."));
        list.Add(Bike("Wave RSX", "demo-wave-rsx", "XeSo", 22_690_000, 110, "Số", false, sort++,
            "Xe số thể thao nhẹ, dễ vận hành."));
        list.Add(Bike("Blade", "demo-blade", "XeSo", 21_390_000, 110, "Số", false, sort++,
            "Xe số phổ thông, chi phí sử dụng thấp."));
        list.Add(Bike("Future 125", "demo-future-125", "XeSo", 31_500_000, 125, "Số", true, sort++,
            "Xe số 125cc êm ái, phù hợp đi đường dài."));
        list.Add(Bike("Super Cub C125", "demo-super-cub-c125", "XeSo", 86_500_000, 125, "Số", true, sort++,
            "Biểu tượng cổ điển, phong cách retro."));

        // ── Xe côn tay (5) ──
        list.Add(Bike("Winner X", "demo-winner-x", "ConTay", 46_160_000, 150, "Côn tay", true, sort++,
            "Underbone thể thao, phù hợp người mới chơi côn."));
        list.Add(Bike("CB150 Verza", "demo-cb150-verza", "ConTay", 42_900_000, 150, "Côn tay", false, sort++,
            "Naked entry, dễ kiểm soát trong phố."));
        list.Add(Bike("CBR150R", "demo-cbr150r", "ConTay", 72_500_000, 150, "Côn tay", true, sort++,
            "Sportbike 150cc đậm chất đường đua."));
        list.Add(Bike("CBR250RR", "demo-cbr250rr", "ConTay", 179_000_000, 250, "Côn tay", true, sort++,
            "Sport 250 phân khúc cao hơn cho người đam mê."));
        list.Add(Bike("Sonic 150R", "demo-sonic-150r", "ConTay", 42_500_000, 150, "Côn tay", false, sort++,
            "Underbone cá tính, phù hợp giới trẻ."));

        // ── Xe phân khối lớn (5) ──
        list.Add(Bike("CB500 Hornet", "demo-cb500-hornet", "PhanKhoiLon", 189_000_000, 500, "Côn tay", true, sort++,
            "Naked mid-size linh hoạt đường phố và tour ngắn."));
        list.Add(Bike("CBR650R", "demo-cbr650r", "PhanKhoiLon", 268_000_000, 650, "Côn tay", true, sort++,
            "Sport mid-size cân bằng hiệu suất và tiện dụng."));
        list.Add(Bike("CB650R", "demo-cb650r", "PhanKhoiLon", 246_000_000, 650, "Côn tay", false, sort++,
            "Neo Sports Café mạnh mẽ, phong cách hiện đại."));
        list.Add(Bike("Africa Twin", "demo-africa-twin", "PhanKhoiLon", 569_000_000, 1084, "Côn tay", true, sort++,
            "Adventure touring — demo phân khúc lớn."));
        list.Add(Bike("Rebel 500", "demo-rebel-500", "PhanKhoiLon", 187_000_000, 500, "Côn tay", false, sort++,
            "Cruiser cá tính, tư thế ngồi thoải mái."));

        // ── Xe điện (5) ──
        list.Add(Bike("ICON e:", "demo-icon-e", "Electric", 21_990_000, null, "Tự động (điện)", true, sort++,
            "Xe điện đô thị nhỏ gọn — dữ liệu demo trình bày."));
        list.Add(Bike("CUV e:", "demo-cuv-e", "Electric", 34_990_000, null, "Tự động (điện)", true, sort++,
            "Xe điện đa dụng, phù hợp đi phố."));
        list.Add(Bike("EM1 e:", "demo-em1-e", "Electric", 29_990_000, null, "Tự động (điện)", false, sort++,
            "Xe điện cá nhân, vận hành êm."));
        list.Add(Bike("Demo Electric 1", "demo-electric-1", "Electric", 19_500_000, null, "Tự động (điện)", false, sort++,
            "Mẫu điện demo bổ sung cho danh mục."));
        list.Add(Bike("Demo Electric 2", "demo-electric-2", "Electric", 27_500_000, null, "Tự động (điện)", false, sort++,
            "Mẫu điện demo thứ hai — thay nội dung trong CMS."));

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
                "Giá, thông số và mô tả có thể chỉnh sửa trong Admin. Ảnh hiện tại là placeholder — thay bằng ảnh thật qua Media.</p>",
            EngineCc = engineCc,
            FuelType = fuel,
            Transmission = transmission,
            Highlights =
            [
                "Dữ liệu demo — chỉnh sửa trong CMS",
                "Đầy đủ media placeholder (gallery / màu / 360°)",
                "Máy tính trả góp bật mặc định"
            ],
            Specifications =
            [
                new DemoSpecItem { Icon = "⚡", Label = "Dung tích / động cơ", Value = engineCc.HasValue ? $"{engineCc} cc" : "Động cơ điện (demo)" },
                new DemoSpecItem { Icon = "⚙️", Label = "Hộp số", Value = transmission },
                new DemoSpecItem { Icon = "⛽", Label = "Nhiên liệu", Value = fuel },
                new DemoSpecItem { Icon = "📉", Label = "Mức tiêu hao", Value = consumption },
                new DemoSpecItem { Icon = "🛡️", Label = "Bảo hành", Value = warranty },
                new DemoSpecItem { Icon = "group", Label = "Thông tin chung", Value = "" },
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
                new DemoColorItem { Name = "Đen", Hex = "#1A1A1A", Image = "black.jpg" },
                new DemoColorItem { Name = "Trắng", Hex = "#F5F5F5", Image = "white.jpg" },
                new DemoColorItem { Name = "Đỏ", Hex = "#E40521", Image = "red.jpg" }
            ],
            Features =
            [
                new DemoContentCard
                {
                    Title = "Thiết kế nổi bật",
                    Description = $"Điểm nhấn thiết kế demo cho {name}. Thay nội dung trong CMS.",
                    Image = "feature-01.jpg"
                },
                new DemoContentCard
                {
                    Title = "Vận hành hàng ngày",
                    Description = "Nội dung feature demo — editable trong tab Features.",
                    Image = "feature-02.jpg"
                }
            ],
            Technology =
            [
                new DemoContentCard
                {
                    Title = "Công nghệ an toàn",
                    Description = "Mô tả technology demo cho trang chi tiết.",
                    Image = "tech-01.jpg"
                },
                new DemoContentCard
                {
                    Title = "Tiện ích thông minh",
                    Description = "Card công nghệ thứ hai — thay bằng nội dung thật sau.",
                    Image = "tech-02.jpg"
                }
            ],
            Seo = new DemoSeoMetadata
            {
                MetaTitle = $"{name} | Xe Máy Hiếu Nga",
                MetaDescription = $"{shortDesc} Giá demo từ {price:N0} ₫ tại showroom Đà Nẵng.",
                MetaKeywords = $"{name}, xe may hieu nga, {CategoryLabel(category)}",
                CanonicalUrl = $"/xe/{slug}"
            },
            Finance = new DemoFinanceDefaults
            {
                CalculatorEnabled = true,
                DefaultDownPaymentPercent = 20,
                DefaultTermMonths = 12
            },
            Assets = new DemoAssetHints()
        };
    }

    private static string CategoryLabel(string category) =>
        DemoPackageCatalog.ParseCategory(category) switch
        {
            MotorcycleCategory.Scooter => "Scooter",
            MotorcycleCategory.XeSo => "Xe số",
            MotorcycleCategory.ConTay => "Xe côn tay",
            MotorcycleCategory.PhanKhoiLon => "Xe phân khối lớn",
            MotorcycleCategory.Electric => "Xe điện",
            _ => category
        };
}
