using HieuNga.Domain.Enums;

namespace HieuNga.Infrastructure.Persistence;

public record MotorcycleColorSeed(string Name, string Hex, string? ImageUrl, int Sort);
public record MotorcycleVariantSeed(string Name, decimal Price, string Sku, int Stock);
public record MotorcycleSpecSeed(string Icon, string Label, string Value);

public record MotorcycleContentProfile(
    string Slug,
    string ShortDescription,
    string DescriptionHtml,
    string Transmission,
    string ThumbnailUrl,
    IReadOnlyList<string> GalleryUrls,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<MotorcycleSpecSeed> Specifications,
    IReadOnlyList<MotorcycleColorSeed> Colors,
    IReadOnlyList<MotorcycleVariantSeed> Variants);

public static class MotorcycleContentCatalog
{
    public static IReadOnlyList<MotorcycleContentProfile> All { get; } =
    [
        VisionProfile(),
        ShProfile(),
        WinnerProfile(),
        Cb150rProfile()
    ];

    public static MotorcycleContentProfile? GetBySlug(string slug) =>
        All.FirstOrDefault(p => p.Slug == slug);

    private static MotorcycleContentProfile VisionProfile() => new(
        "honda-vision-2025",
        "Tay ga đô thị bán chạy nhất — tiết kiệm nhiên liệu, cốp rộng, đèn LED toàn phần. Lý tưởng đi làm, đưa con, chạy nội thành Đà Nẵng.",
        """
        <p><strong>Honda Vision 2025</strong> tiếp tục khẳng định vị thế mẫu tay ga bán chạy nhất phân khúc với động cơ eSP+ 110cc, tiết kiệm nhiên liệu và vận hành bền bỉ.</p>
        <h3>Vì sao chọn Vision?</h3>
        <ul>
        <li>Thiết kế trẻ trung, thanh lịch — phù hợp cả nam và nữ</li>
        <li>Cốp rộng 18 lít, đựng mũ bảo hiểm 3/4 thoải mái</li>
        <li>Hệ thống đèn LED phía trước và sau — tầm nhìn tốt ban đêm</li>
        <li>Tiêu hao nhiên liệu khoảng 1,8–2,0 lít/100km (điều kiện đô thị)</li>
        </ul>
        <h3>Phù hợp với ai?</h3>
        <p>Sinh viên, nhân viên văn phòng, gia đình cần xe tiện dụng di chuyển hằng ngày tại Đà Nẵng. Dễ lái, dễ bảo dưỡng, chi phí sở hữu hợp lý.</p>
        <h3>Công nghệ Honda</h3>
        <p>Động cơ eSP+ tối ưu ma sát, khung thép ống vuông vững chắc, phanh CBS an toàn (bản CBS), móc treo đồ tiện lợi.</p>
        """,
        "Hộp số tự động",
        "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1200&q=85",
        [
            "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1400&q=85",
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1400&q=85",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=1400&q=85",
            "https://images.unsplash.com/photo-1609630875171-989e756aecf8?w=1400&q=85",
            "https://images.unsplash.com/photo-1625047509168-a7026f36de0c?w=1400&q=85"
        ],
        [
            "Động cơ eSP+ 110cc tiết kiệm nhiên liệu",
            "Cốp rộng 18 lít — đực mũ bảo hiểm",
            "Đèn LED toàn phần",
            "Dễ điều khiển trong phố",
            "Giá sở hữu hợp lý, trả góp linh hoạt"
        ],
        VisionSpecs(),
        [
            new("Trắng ngọc trai", "#F5F5F5", "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=600&q=80", 0),
            new("Đen bóng", "#1A1A1A", "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=600&q=80", 1),
            new("Đỏ đen", "#8B0000", "https://images.unsplash.com/photo-1609630875171-989e756aecf8?w=600&q=80", 2)
        ],
        [
            new("Vision 2025", 35_900_000, "VIS-2025-STD", 8),
            new("Vision 2025 CBS", 37_400_000, "VIS-2025-CBS", 5)
        ]);

    private static MotorcycleContentProfile ShProfile() => new(
        "honda-sh-160i",
        "Tay ga cao cấp — động cơ 160cc mạnh mẽ, thiết kế châu Âu, công nghệ Smart Key & LED toàn phần.",
        """
        <p><strong>Honda SH 160i</strong> là biểu tượng tay ga cao cấp tại Việt Nam với động cơ 160cc, hộp số eSP+, thiết kế thanh lịch và trang bị hiện đại.</p>
        <h3>Điểm nhấn nổi bật</h3>
        <ul>
        <li>Động cơ 160cc — tăng tốc tự tin khi vào đường trường</li>
        <li>Smart Key thông minh — khóa/mở không cần chìa</li>
        <li>Màn hình LCD đa thông tin</li>
        <li>Phanh ABS (tuỳ phiên bản) — an toàn khi mưa</li>
        </ul>
        <h3>Trải nghiệm lái</h3>
        <p>SH mang lại cảm giác lái chắc chắn, im lặng, phù hợp người yêu thích sự chỉn chu và tiện nghi khi di chuyển trong thành phố.</p>
        """,
        "Hộp số tự động eSP+",
        "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1200&q=85",
        [
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1400&q=85",
            "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1400&q=85",
            "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=1400&q=85",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=1400&q=85",
            "https://images.unsplash.com/photo-1609630875171-989e756aecf8?w=1400&q=85"
        ],
        [
            "Động cơ eSP+ 160cc",
            "Smart Key tiện lợi",
            "Thiết kế tay ga cao cấp",
            "Cốp rộng, đèn LED",
            "Phù hợp đi làm & dạo phố"
        ],
        ShSpecs(),
        [
            new("Trắng pearl", "#F8F8F8", null, 0),
            new("Xám xi măng", "#6B7280", null, 1),
            new("Đen", "#111111", null, 2)
        ],
        [
            new("SH 160i", 78_500_000, "SH160-STD", 4),
            new("SH 160i ABS", 82_900_000, "SH160-ABS", 2)
        ]);

    private static MotorcycleContentProfile WinnerProfile() => new(
        "honda-winner-x",
        "Xe côn tay ga thể thao — động cơ 150cc SOHC, thiết kế trẻ trung, phù hợp người yêu tốc độ & phong cách.",
        """
        <p><strong>Honda Winner X</strong> dành cho người trẻ năng động — kiểu dáng thể thao, động cơ 150cc mạnh mẽ, hộp số 6 cấp mang lại cảm giác lái phấn khích.</p>
        <h3>Ưu điểm</h3>
        <ul>
        <li>Động cơ 150cc SOHC — tăng tốc nhanh</li>
        <li>Khung thép ống vuông cứng vững</li>
        <li>Đèn LED định vị ban ngày</li>
        <li>Giá thành cạnh tranh trong phân khúc</li>
        </ul>
        <p>Phù hợp sinh viên, nhân viên trẻ, người thích xe côn thể thao nhưng vẫn tiện dụng hằng ngày.</p>
        """,
        "Hộp số cơ 6 cấp",
        "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=1200&q=85",
        [
            "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=1400&q=85",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=1400&q=85",
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1400&q=85",
            "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1400&q=85",
            "https://images.unsplash.com/photo-1625047509168-a7026f36de0c?w=1400&q=85"
        ],
        [
            "Động cơ 150cc SOHC",
            "Kiểu dáng thể thao trẻ trung",
            "Hộp số 6 cấp",
            "Giá tốt phân khúc",
            "Dễ nâng cấp phụ kiện"
        ],
        WinnerSpecs(),
        [
            new("Đỏ đen", "#B91C1C", "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=600&q=85", 0),
            new("Xanh GP", "#1E3A5F", "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=600&q=85", 1),
            new("Đen", "#0F0F0F", "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=600&q=85", 2)
        ],
        [
            new("Winner X", 46_500_000, "WIN-X-STD", 10),
            new("Winner X V3", 47_900_000, "WIN-X-V3", 6)
        ]);

    private static MotorcycleContentProfile Cb150rProfile() => new(
        "honda-cb150r",
        "Naked bike thể thao — động cơ 150cc PGM-FI, thiết kế Neo Sports Café, phong cách châu Âu năng động.",
        """
        <p><strong>Honda CB150R</strong> mang DNA thể thao Honda với thiết kế Neo Sports Café — gọn gàng, cá tính, phù hợp người yêu naked bike trong phố.</p>
        <h3>Trang bị & công nghệ</h3>
        <ul>
        <li>Động cơ 150cc PGM-FI</li>
        <li>Đèn LED full</li>
        <li>Phanh đĩa trước/sau</li>
        <li>Tay lái clip-on thể thao</li>
        </ul>
        <p>Lý tưởng cho người thích phong cách cafe racer, di chuyển linh hoạt và chụp ảnh cùng xe.</p>
        """,
        "Hộp số cơ 6 cấp",
        "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=1200&q=85",
        [
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=1400&q=85",
            "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=1400&q=85",
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1400&q=85",
            "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1400&q=85",
            "https://images.unsplash.com/photo-1609630875171-989e756aecf8?w=1400&q=85"
        ],
        [
            "Thiết kế Neo Sports Café",
            "Động cơ PGM-FI 150cc",
            "Nhẹ, linh hoạt trong phố",
            "Đèn LED hiện đại",
            "Phong cách naked thể thao"
        ],
        Cb150rSpecs(),
        [
            new("Đỏ", "#DC2626", "https://images.unsplash.com/photo-1568772585407-9361f9bf3a83?w=600&q=85", 0),
            new("Xanh đen", "#1E293B", "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=600&q=85", 1),
            new("Đen matt", "#27272A", "https://images.unsplash.com/photo-1609630875171-989e756aecf8?w=600&q=85", 2)
        ],
        [
            new("CB150R", 52_000_000, "CB150R-STD", 3)
        ]);

    private static IReadOnlyList<MotorcycleSpecSeed> VisionSpecs() =>
    [
        new("⚡", "Dung tích xy-lanh", "110 cc"),
        new("⚙️", "Hộp số", "Tự động"),
        new("⛽", "Dung tích bình xăng", "4,2 lít"),
        new("📏", "Chiều cao yên", "754 mm"),
        new("⚖️", "Trọng lượng", "96 kg"),
        new("🔋", "Tiêu hao NL", "~1,8 lít/100km"),
        new("↔️", "Chiều dài cơ sở", "1.280 mm"),
        new("📐", "Kích thước D×R×C", "1.890×700×1.115 mm"),
        new("🛑", "Phanh", "Đĩa / CBS (bản CBS)"),
        new("🔩", "Giảm xóc", "Ống lồng / Lò xo"),
        new("🛞", "Lốp", "80/90-14 (trước) · 90/90-14 (sau)")
    ];

    private static IReadOnlyList<MotorcycleSpecSeed> ShSpecs() =>
    [
        new("⚡", "Dung tích xy-lanh", "160 cc"),
        new("⚙️", "Hộp số", "Tự động eSP+"),
        new("⛽", "Dung tích bình xăng", "6,0 lít"),
        new("📏", "Chiều cao yên", "785 mm"),
        new("⚖️", "Trọng lượng", "133 kg"),
        new("🔋", "Tiêu hao NL", "~2,2 lít/100km"),
        new("↔️", "Chiều dài cơ sở", "1.353 mm"),
        new("📐", "Kích thước D×R×C", "2.013×730×1.151 mm"),
        new("🛑", "Phanh", "Đĩa / ABS (bản ABS)"),
        new("🔩", "Giảm xóc", "Ống lồng / Lò xo"),
        new("🛞", "Lốp", "110/70-16 · 130/70-16")
    ];

    private static IReadOnlyList<MotorcycleSpecSeed> WinnerSpecs() =>
    [
        new("⚡", "Dung tích xy-lanh", "150 cc"),
        new("⚙️", "Hộp số", "Cơ 6 cấp"),
        new("⛽", "Dung tích bình xăng", "4,5 lít"),
        new("📏", "Chiều cao yên", "775 mm"),
        new("⚖️", "Trọng lượng", "122 kg"),
        new("🔋", "Tiêu hao NL", "~2,0 lít/100km"),
        new("↔️", "Chiều dài cơ sở", "1.324 mm"),
        new("📐", "Kích thước D×R×C", "2.020×730×1.074 mm"),
        new("🛑", "Phanh", "Đĩa trước & sau"),
        new("🔩", "Giảm xóc", "Ống lồng / Lò xo"),
        new("🛞", "Lốp", "80/90-17 · 110/70-17")
    ];

    private static IReadOnlyList<MotorcycleSpecSeed> Cb150rSpecs() =>
    [
        new("⚡", "Dung tích xy-lanh", "150 cc"),
        new("⚙️", "Hộp số", "Cơ 6 cấp"),
        new("⛽", "Dung tích bình xăng", "12,0 lít"),
        new("📏", "Chiều cao yên", "795 mm"),
        new("⚖️", "Trọng lượng", "131 kg"),
        new("🔋", "Tiêu hao NL", "~2,1 lít/100km"),
        new("↔️", "Chiều dài cơ sở", "1.301 mm"),
        new("📐", "Kích thước D×R×C", "2.020×790×1.055 mm"),
        new("🛑", "Phanh", "Đĩa trước & sau"),
        new("🔩", "Giảm xóc", "Ống lồng / Lò xo đơn"),
        new("🛞", "Lốp", "110/70-17 · 150/60-17")
    ];
}
