namespace HieuNga.Application.Catalog;

/// <summary>Flagship HEAD service experiences for public /dich-vu.</summary>
public static class HieuNgaServiceExperience
{
    public const string CategoryName = "Dịch vụ HEAD";
    public const string CategorySlug = "dich-vu-head";

    public static IReadOnlyList<ServiceExperienceDef> All { get; } =
    [
        new(
            "sua-chua-thay-the-phu-tung",
            "Sửa chữa & thay thế phụ tùng",
            "Phụ tùng Honda chính hãng — chẩn đoán rõ ràng, báo giá trước khi thay.",
            "Xưởng HEAD Hiếu Nga sửa chữa và thay thế phụ tùng theo quy trình Honda Việt Nam. Kỹ thuật viên kiểm tra, tư vấn hạng mục cần làm và chỉ thay khi thực sự cần thiết.",
            "repair",
            "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=1200&q=80",
            "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=1800&q=80",
            ["Phụ tùng chính hãng Honda", "Báo giá minh bạch trước khi sửa", "Bảo hành hạng mục thay thế", "Kỹ thuật viên đào tạo HEAD"],
            ["Xe có tiếng kêu bất thường", "Hạng mục hỏng / mòn theo km", "Cần thay thế phụ tùng định kỳ"],
            ["Tiếp nhận & lắng nghe hiện tượng", "Chẩn đoán tại xưởng", "Báo giá & xác nhận với khách", "Thay thế / sửa chữa", "Kiểm tra lại trước khi giao xe"],
            [
                ("Dùng phụ tùng chính hãng không?", "Có — ưu tiên phụ tùng Honda chính hãng. Nếu có phương án thay thế, chúng tôi sẽ tư vấn rõ trước."),
                ("Có báo giá trước không?", "Có. Sau khi kiểm tra, xưởng báo giá và chỉ thực hiện khi bạn đồng ý."),
                ("Thời gian sửa chữa bao lâu?", "Tùy hạng mục — từ vài chục phút đến trong ngày. Nhân viên sẽ ước tính khi tiếp nhận.")
            ],
            1),
        new(
            "bao-hanh-bao-duong",
            "Bảo hành & bảo dưỡng",
            "Bảo dưỡng định kỳ theo km — giữ bảo hành và vận hành ổn định.",
            "Gói bảo dưỡng HEAD giúp xe vận hành bền bỉ và đúng lịch bảo hành Honda. Kiểm tra tổng thể, thay nhớt/lọc theo khuyến nghị và ghi nhận lịch sử bảo dưỡng.",
            "wrench",
            "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=1200&q=80",
            "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=1800&q=80",
            ["Đúng quy trình Honda Việt Nam", "Ghi nhận lịch sử bảo dưỡng", "Nhắc lịch bảo dưỡng tiếp theo", "Không phát sinh chi phí ẩn"],
            ["Đến mốc km bảo dưỡng", "Xe mới cần bảo dưỡng lần đầu", "Chuẩn bị đi xa / mùa mưa"],
            ["Đặt lịch trước", "Tiếp nhận xe & kiểm tra", "Thực hiện hạng mục bảo dưỡng", "Vệ sinh / kiểm tra an toàn", "Bàn giao & tư vấn lần sau"],
            [
                ("Bảo dưỡng có ảnh hưởng bảo hành?", "Bảo dưỡng đúng quy trình tại HEAD giúp duy trì điều kiện bảo hành theo chính sách Honda."),
                ("Cần mang gì khi đến?", "Giấy tờ xe (nếu có) và mô tả hiện tượng bạn muốn kiểm tra thêm."),
                ("Có đặt lịch online không?", "Có — đặt lịch trên website hoặc gọi hotline showroom.")
            ],
            2),
        new(
            "dau-nhot-chinh-hang",
            "Dầu nhớt chính hãng",
            "Nhớt Honda đúng định mức — bảo vệ động cơ theo khuyến nghị hãng.",
            "Thay nhớt máy và nhớt hộp số (xe ga) bằng sản phẩm phù hợp từng dòng xe. Kiểm tra rò rỉ, mức nhớt và tư vấn chu kỳ thay tiếp theo.",
            "oil",
            "https://images.unsplash.com/photo-1625047509168-a7026f773785?w=1200&q=80",
            "https://images.unsplash.com/photo-1625047509168-a7026f773785?w=1800&q=80",
            ["Nhớt chính hãng / đúng chủng loại", "Xả nhớt kỹ thuật", "Kiểm tra rò rỉ sau thay", "Tư vấn chu kỳ thay nhớt"],
            ["Đến hạn thay nhớt", "Nhớt đen / thiếu nhớt", "Sau hành trình dài"],
            ["Tư vấn loại nhớt phù hợp", "Xả nhớt cũ", "Thay nhớt mới đúng định mức", "Kiểm tra lại & bàn giao"],
            [
                ("Dùng nhớt ngoài có được không?", "Có thể — kỹ thuật viên sẽ tư vấn loại phù hợp. Khuyến nghị nhớt đúng chuẩn Honda cho từng dòng xe."),
                ("Xe ga có cần nhớt hộp số?", "Có — nhớt hộp số xe ga cần thay theo khuyến nghị để nồi / truyền động bền hơn."),
                ("Thay nhớt mất bao lâu?", "Thường 20–40 phút tùy dòng xe và hạng mục kèm theo.")
            ],
            3),
        new(
            "sua-chua-luu-dong",
            "Sửa chữa lưu động",
            "Hỗ trợ tại chỗ theo khu vực — xử lý nhanh sự cố nhỏ trên đường.",
            "Đội lưu động hỗ trợ các tình huống không đến được xưởng ngay: hết bình, sự cố nhẹ, hỗ trợ kéo/đưa về HEAD khi cần. Phạm vi theo khu vực Đà Nẵng & thỏa thuận.",
            "truck",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=1200&q=80",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=1800&q=80",
            ["Tiếp nhận nhanh qua hotline", "Hỗ trợ tại chỗ theo khu vực", "Tư vấn mang về xưởng nếu cần", "Ưu tiên an toàn trên đường"],
            ["Xe không nổ máy / hết bình", "Sự cố nhẹ trên đường", "Không thể đẩy xe đến xưởng"],
            ["Gọi hotline / đặt yêu cầu", "Xác nhận vị trí & hiện tượng", "Điều phối kỹ thuật", "Xử lý tại chỗ hoặc đưa về HEAD"],
            [
                ("Phạm vi lưu động ở đâu?", "Ưu tiên khu vực Đà Nẵng. Ngoài khu vực sẽ báo phí/thời gian trước khi đến."),
                ("Có sửa lớn tại chỗ không?", "Sự cố lớn sẽ tư vấn đưa về xưởng để đảm bảo thiết bị và an toàn."),
                ("Chi phí tính thế nào?", "Báo phí di chuyển / công tác trước khi triển khai.")
            ],
            4),
        new(
            "bao-hiem-xe-may",
            "Bảo hiểm xe máy",
            "Tư vấn gói bảo hiểm phù hợp khi mua xe hoặc gia hạn.",
            "Nhân viên tư vấn các gói bảo hiểm phổ biến (bắt buộc / vật chất) theo nhu cầu sử dụng, giúp bạn chọn mức phù hợp ngân sách và quyền lợi.",
            "shield",
            "https://images.unsplash.com/photo-1450101499163-c8848c66ca85?w=1200&q=80",
            "https://images.unsplash.com/photo-1450101499163-c8848c66ca85?w=1800&q=80",
            ["Tư vấn rõ quyền lợi", "Hỗ trợ khi mua xe mới", "Gợi ý mức phù hợp ngân sách", "Không ép mua gói không cần"],
            ["Mua xe mới tại showroom", "Sắp hết hạn bảo hiểm", "Muốn nâng quyền lợi vật chất"],
            ["Tìm hiểu nhu cầu sử dụng", "Giới thiệu các gói phù hợp", "So sánh quyền lợi / phí", "Hỗ trợ hoàn tất thủ tục"],
            [
                ("Bảo hiểm bắt buộc có bắt buộc không?", "Theo quy định hiện hành, xe tham gia giao thông cần bảo hiểm trách nhiệm dân sự bắt buộc."),
                ("Mua bảo hiểm tại Hiếu Nga được không?", "Có — chúng tôi hỗ trợ tư vấn và thủ tục tại showroom."),
                ("Có bắt buộc mua kèm khi mua xe?", "Không ép. Bạn được tư vấn để tự quyết định.")
            ],
            5),
        new(
            "tan-trang-cham-soc-xe",
            "Tân trang & chăm sóc xe",
            "Làm đẹp – bảo dưỡng ngoại thất, giữ xe luôn như mới.",
            "Dịch vụ chăm sóc xe: vệ sinh, bảo dưỡng ngoại thất và các hạng mục làm mới giúp xe sạch sẽ, bền màu và dễ bảo quản hàng ngày.",
            "sparkle",
            "https://images.unsplash.com/photo-1615172282427-9a57ef2d0cf3?w=1200&q=80",
            "https://images.unsplash.com/photo-1615172282427-9a57ef2d0cf3?w=1800&q=80",
            ["Vệ sinh chuyên sâu", "Chăm sóc ngoại thất", "Tư vấn bảo quản tại nhà", "Phù hợp xe đi phố hàng ngày"],
            ["Xe bẩn sau mùa mưa", "Muốn làm mới ngoại thất", "Chuẩn bị bán / giao xe"],
            ["Tiếp nhận & khảo sát tình trạng", "Tư vấn gói chăm sóc", "Thực hiện vệ sinh / chăm sóc", "Kiểm tra hoàn thiện & bàn giao"],
            [
                ("Có làm được xe rất bẩn không?", "Có — kỹ thuật viên sẽ khảo sát và báo hạng mục phù hợp."),
                ("Có ảnh hưởng sơn zin không?", "Chúng tôi dùng quy trình / hóa chất phù hợp, ưu tiên bảo vệ bề mặt zin."),
                ("Đặt lịch trước có cần không?", "Nên đặt trước để xếp lịch xưởng, đặc biệt cuối tuần.")
            ],
            6)
    ];

    public static readonly HashSet<string> FlagshipSlugs =
        new(All.Select(x => x.Slug), StringComparer.OrdinalIgnoreCase);

    /// <summary>Legacy demo item slugs from older seed — deactivated from public listing.</summary>
    public static readonly HashSet<string> LegacyDemoSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bao-duong-dinh-ky", "thay-nhot-may", "thay-nhot-hop-so-xe-ga", "kiem-tra-loc-gio",
        "kiem-tra-bugi", "kiem-tra-phanh", "kiem-tra-lop", "kiem-tra-dien-binh-ac-quy",
        "kiem-tra-dong-co", "ve-sinh-kim-phun-buong-dot", "kiem-tra-day-curoa-noi-xe-ga",
        "sua-chua-tong-quat", "thay-phu-tung-chinh-hang", "kiem-tra-xe-truoc-chuyen-di"
    };
}

public sealed record ServiceExperienceDef(
    string Slug,
    string Name,
    string Summary,
    string Detail,
    string IconKey,
    string ThumbnailUrl,
    string HeroImageUrl,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> Process,
    IReadOnlyList<(string Q, string A)> Faqs,
    int Order);
