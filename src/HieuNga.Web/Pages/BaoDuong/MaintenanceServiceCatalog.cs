namespace HieuNga.Web.Pages.BaoDuong;

public record MaintenanceServiceDisplay(
    string Slug,
    string Name,
    string Category,
    string IconKey,
    string ShortDescription,
    IReadOnlyList<string> Includes,
    string EstimatedPrice,
    string EstimatedDuration,
    string? PriceNote = null);

public static class MaintenanceServiceCatalog
{
    public const string PricingDisclaimer =
        "Giá có thể thay đổi theo dòng xe, tình trạng xe và phụ tùng thực tế. Honda Hiếu Nga sẽ kiểm tra và báo giá rõ ràng trước khi thực hiện.";

    public static IReadOnlyList<string> BookingServiceOptions =>
        Services.Select(s => s.Name).ToList();

    public static IReadOnlyList<MaintenanceServiceDisplay> Services { get; } =
    [
        new(
            "bao-duong-dinh-ky",
            "Bảo dưỡng định kỳ",
            "Bảo dưỡng",
            "wrench",
            "Kiểm tra tổng quát xe theo mốc km để xe vận hành ổn định.",
            [
                "Kiểm tra phanh, lốp, đèn, còi, xích/dây curoa.",
                "Kiểm tra nhớt, lọc gió, bugi.",
                "Tư vấn hạng mục cần thay thế nếu có."
            ],
            "Từ 150.000đ – 350.000đ",
            "30 – 60 phút",
            "Giá tham khảo — Honda Hiếu Nga xác nhận trước khi thực hiện."),
        new(
            "thay-nhot-may",
            "Thay nhớt máy",
            "Bảo dưỡng",
            "oil",
            "Thay nhớt phù hợp với dòng xe số, xe ga hoặc xe côn.",
            [
                "Xả nhớt cũ.",
                "Thay nhớt mới theo khuyến nghị.",
                "Kiểm tra rò rỉ và mức nhớt."
            ],
            "Từ 120.000đ – 250.000đ",
            "15 – 25 phút"),
        new(
            "thay-nhot-hop-so-xe-ga",
            "Thay nhớt hộp số xe ga",
            "Bảo dưỡng",
            "oil-gear",
            "Thay nhớt láp/hộp số cho xe tay ga.",
            [
                "Xả nhớt láp cũ.",
                "Thay nhớt hộp số mới.",
                "Kiểm tra tiếng ồn bất thường nếu có."
            ],
            "Từ 60.000đ – 120.000đ",
            "15 – 20 phút"),
        new(
            "kiem-tra-loc-gio",
            "Kiểm tra / thay lọc gió",
            "Bảo dưỡng",
            "filter",
            "Kiểm tra tình trạng lọc gió, vệ sinh hoặc thay mới khi cần.",
            [
                "Tháo kiểm tra lọc gió.",
                "Vệ sinh nhẹ nếu còn dùng được.",
                "Tư vấn thay lọc gió nếu quá bẩn hoặc hư hỏng."
            ],
            "Từ 80.000đ – 180.000đ",
            "15 – 30 phút"),
        new(
            "kiem-tra-bugi",
            "Kiểm tra / thay bugi",
            "Bảo dưỡng",
            "spark",
            "Kiểm tra bugi để xe dễ nổ máy, vận hành ổn định hơn.",
            [
                "Tháo kiểm tra bugi.",
                "Vệ sinh hoặc thay mới nếu cần.",
                "Kiểm tra tình trạng đánh lửa cơ bản."
            ],
            "Từ 70.000đ – 180.000đ",
            "15 – 25 phút"),
        new(
            "kiem-tra-phanh",
            "Kiểm tra phanh / thay má phanh",
            "An toàn",
            "brake",
            "Kiểm tra hệ thống phanh trước/sau, má phanh và dầu phanh nếu có.",
            [
                "Kiểm tra độ mòn má phanh.",
                "Kiểm tra hành trình tay phanh/chân phanh.",
                "Tư vấn thay má phanh hoặc bảo dưỡng phanh."
            ],
            "Kiểm tra từ 0đ – 50.000đ, thay má phanh từ 150.000đ – 350.000đ",
            "20 – 45 phút"),
        new(
            "kiem-tra-lop",
            "Kiểm tra lốp / vá lốp / thay lốp",
            "An toàn",
            "tire",
            "Kiểm tra áp suất, độ mòn và tình trạng lốp.",
            [
                "Kiểm tra áp suất lốp.",
                "Kiểm tra nứt, mòn, đinh hoặc thủng.",
                "Vá lốp hoặc tư vấn thay lốp nếu cần."
            ],
            "Vá lốp từ 30.000đ – 80.000đ, thay lốp báo giá theo loại lốp",
            "15 – 45 phút"),
        new(
            "kiem-tra-dien-binh-ac-quy",
            "Kiểm tra điện / bình ắc quy",
            "Điện",
            "battery",
            "Kiểm tra khả năng đề máy, hệ thống sạc, bình ắc quy và đèn.",
            [
                "Kiểm tra điện áp bình.",
                "Kiểm tra sạc cơ bản.",
                "Kiểm tra đèn, còi, xi nhan.",
                "Tư vấn thay bình nếu bình yếu."
            ],
            "Kiểm tra từ 50.000đ – 100.000đ, thay bình báo giá theo loại bình",
            "20 – 40 phút"),
        new(
            "kiem-tra-dong-co",
            "Kiểm tra động cơ",
            "Sửa chữa",
            "engine",
            "Kiểm tra các dấu hiệu máy yếu, khó nổ, hao xăng hoặc tiếng máy bất thường.",
            [
                "Nghe và kiểm tra tình trạng vận hành.",
                "Kiểm tra bugi, lọc gió, nhớt cơ bản.",
                "Tư vấn bước sửa chữa tiếp theo nếu cần tháo kiểm tra sâu."
            ],
            "Từ 100.000đ – 300.000đ, sửa chữa phát sinh sẽ báo giá riêng",
            "30 – 60 phút"),
        new(
            "ve-sinh-kim-phun-buong-dot",
            "Vệ sinh kim phun / buồng đốt",
            "Sửa chữa",
            "inject",
            "Hỗ trợ xe vận hành mượt hơn khi có dấu hiệu hụt ga, hao xăng hoặc máy không đều.",
            [
                "Kiểm tra tình trạng vận hành.",
                "Vệ sinh theo quy trình phù hợp.",
                "Tư vấn thêm nếu phát hiện lỗi liên quan."
            ],
            "Từ 150.000đ – 350.000đ",
            "30 – 60 phút"),
        new(
            "kiem-tra-day-curoa-noi-xe-ga",
            "Kiểm tra dây curoa / nồi xe ga",
            "Xe tay ga",
            "belt",
            "Kiểm tra bộ truyền động xe tay ga khi xe ì, rung đầu hoặc lên ga không mượt.",
            [
                "Kiểm tra dây curoa.",
                "Kiểm tra nồi trước/nồi sau cơ bản.",
                "Tư vấn vệ sinh hoặc thay thế nếu cần."
            ],
            "Kiểm tra từ 100.000đ – 250.000đ, phụ tùng báo giá riêng",
            "30 – 75 phút"),
        new(
            "sua-chua-tong-quat",
            "Sửa chữa tổng quát",
            "Sửa chữa",
            "repair",
            "Tiếp nhận các lỗi phát sinh như xe khó nổ, chết máy, tiếng kêu lạ, hao xăng, rung giật.",
            [
                "Tiếp nhận tình trạng xe.",
                "Kiểm tra ban đầu.",
                "Báo lỗi dự kiến và chi phí trước khi sửa."
            ],
            "Kiểm tra từ 0đ – 100.000đ, sửa chữa báo giá sau kiểm tra",
            "Tùy tình trạng xe"),
        new(
            "thay-phu-tung-chinh-hang",
            "Thay phụ tùng chính hãng",
            "Phụ tùng",
            "parts",
            "Tư vấn và thay thế phụ tùng phù hợp với từng dòng xe.",
            [
                "Kiểm tra phụ tùng cần thay.",
                "Tư vấn phụ tùng phù hợp.",
                "Báo giá trước khi thay."
            ],
            "Báo giá theo phụ tùng thực tế",
            "Tùy phụ tùng"),
        new(
            "kiem-tra-xe-truoc-chuyen-di",
            "Kiểm tra xe trước chuyến đi",
            "An toàn",
            "trip",
            "Kiểm tra nhanh các hạng mục quan trọng trước khi đi xa.",
            [
                "Kiểm tra phanh, lốp, đèn, còi.",
                "Kiểm tra nhớt và rò rỉ cơ bản.",
                "Tư vấn xử lý các hạng mục rủi ro."
            ],
            "Từ 100.000đ – 200.000đ",
            "20 – 40 phút")
    ];

    public static MaintenanceServiceDisplay? FindBySlug(string slug) =>
        Services.FirstOrDefault(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<MaintenanceServiceDisplay> GetRelated(string slug, int count = 3)
    {
        var current = FindBySlug(slug);
        if (current is null) return [];
        return Services
            .Where(s => s.Slug != slug && s.Category == current.Category)
            .Take(count)
            .ToList();
    }
}
