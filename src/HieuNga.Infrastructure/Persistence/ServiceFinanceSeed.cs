using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

public static class ServiceFinanceSeed
{
    public static async Task SeedAsync(HieuNgaDbContext context, ILogger logger)
    {
        if (!await context.ServiceCategories.AnyAsync())
        {
            logger.LogInformation("Seeding service catalog...");
            await SeedServicesAsync(context);
        }

        if (!await context.BankTypes.AnyAsync())
        {
            logger.LogInformation("Seeding finance banks and rates...");
            await SeedFinanceAsync(context);
        }

        await SeedSiteSettingDefaultsAsync(context, logger);
        await context.SaveChangesAsync();
    }

    private static async Task SeedServicesAsync(HieuNgaDbContext context)
    {
        var categories = new Dictionary<string, ServiceCategory>(StringComparer.OrdinalIgnoreCase);
        void EnsureCategory(string name, string slug, int order)
        {
            if (!categories.ContainsKey(name))
                categories[name] = new ServiceCategory { Name = name, Slug = slug, DisplayOrder = order, IsActive = true };
        }

        EnsureCategory("Bảo dưỡng", "bao-duong", 1);
        EnsureCategory("An toàn", "an-toan", 2);
        EnsureCategory("Điện", "dien", 3);
        EnsureCategory("Sửa chữa", "sua-chua", 4);
        EnsureCategory("Xe tay ga", "xe-tay-ga", 5);
        EnsureCategory("Phụ tùng", "phu-tung", 6);
        context.ServiceCategories.AddRange(categories.Values);
        await context.SaveChangesAsync();

        var items = BuildServiceItems();
        foreach (var def in items)
        {
            var cat = categories[def.CategoryName];
            context.ServiceItems.Add(new ServiceItem
            {
                ServiceCategoryId = cat.Id,
                Name = def.Name,
                Slug = def.Slug,
                ShortDescription = def.ShortDescription,
                DetailDescription = def.ShortDescription,
                IncludesJson = ServiceItemJson.SerializeIncludes(def.Includes),
                EstimatedPriceText = def.Price,
                EstimatedDurationText = def.Duration,
                PriceNote = def.PriceNote,
                IconKey = def.IconKey,
                DisplayOrder = def.Order,
                IsFeatured = def.Featured,
                IsActive = true
            });
        }
    }

    private static ServiceSeedDef[] BuildServiceItems() =>
    [
        Item("bao-duong-dinh-ky", "Bảo dưỡng định kỳ", "Bảo dưỡng", "wrench",
            "Kiểm tra tổng quát xe theo mốc km để xe vận hành ổn định.",
            ["Kiểm tra phanh, lốp, đèn, còi, xích/dây curoa.", "Kiểm tra nhớt, lọc gió, bugi.", "Tư vấn hạng mục cần thay thế nếu có."],
            "Từ 150.000đ – 350.000đ", "30 – 60 phút", "Giá tham khảo — Honda Hiếu Nga xác nhận trước khi thực hiện.", 1, true),
        Item("thay-nhot-may", "Thay nhớt máy", "Bảo dưỡng", "oil",
            "Thay nhớt phù hợp với dòng xe số, xe ga hoặc xe côn.",
            ["Xả nhớt cũ.", "Thay nhớt mới theo khuyến nghị.", "Kiểm tra rò rỉ và mức nhớt."],
            "Từ 120.000đ – 250.000đ", "15 – 25 phút", null, 2, false),
        Item("thay-nhot-hop-so-xe-ga", "Thay nhớt hộp số xe ga", "Bảo dưỡng", "oil-gear",
            "Thay nhớt láp/hộp số cho xe tay ga.",
            ["Xả nhớt láp cũ.", "Thay nhớt hộp số mới.", "Kiểm tra tiếng ồn bất thường nếu có."],
            "Từ 60.000đ – 120.000đ", "15 – 20 phút", null, 3, false),
        Item("kiem-tra-loc-gio", "Kiểm tra / thay lọc gió", "Bảo dưỡng", "filter",
            "Kiểm tra tình trạng lọc gió, vệ sinh hoặc thay mới khi cần.",
            ["Tháo kiểm tra lọc gió.", "Vệ sinh nhẹ nếu còn dùng được.", "Tư vấn thay lọc gió nếu quá bẩn hoặc hư hỏng."],
            "Từ 80.000đ – 180.000đ", "15 – 30 phút", null, 4, false),
        Item("kiem-tra-bugi", "Kiểm tra / thay bugi", "Bảo dưỡng", "spark",
            "Kiểm tra bugi để xe dễ nổ máy, vận hành ổn định hơn.",
            ["Tháo kiểm tra bugi.", "Vệ sinh hoặc thay mới nếu cần.", "Kiểm tra tình trạng đánh lửa cơ bản."],
            "Từ 70.000đ – 180.000đ", "15 – 25 phút", null, 5, false),
        Item("kiem-tra-phanh", "Kiểm tra phanh / thay má phanh", "An toàn", "brake",
            "Kiểm tra hệ thống phanh trước/sau, má phanh và dầu phanh nếu có.",
            ["Kiểm tra độ mòn má phanh.", "Kiểm tra hành trình tay phanh/chân phanh.", "Tư vấn thay má phanh hoặc bảo dưỡng phanh."],
            "Kiểm tra từ 0đ – 50.000đ, thay má phanh từ 150.000đ – 350.000đ", "20 – 45 phút", null, 6, false),
        Item("kiem-tra-lop", "Kiểm tra lốp / vá lốp / thay lốp", "An toàn", "tire",
            "Kiểm tra áp suất, độ mòn và tình trạng lốp.",
            ["Kiểm tra áp suất lốp.", "Kiểm tra nứt, mòn, đinh hoặc thủng.", "Vá lốp hoặc tư vấn thay lốp nếu cần."],
            "Vá lốp từ 30.000đ – 80.000đ, thay lốp báo giá theo loại lốp", "15 – 45 phút", null, 7, false),
        Item("kiem-tra-dien-binh-ac-quy", "Kiểm tra điện / bình ắc quy", "Điện", "battery",
            "Kiểm tra khả năng đề máy, hệ thống sạc, bình ắc quy và đèn.",
            ["Kiểm tra điện áp bình.", "Kiểm tra sạc cơ bản.", "Kiểm tra đèn, còi, xi nhan.", "Tư vấn thay bình nếu bình yếu."],
            "Kiểm tra từ 50.000đ – 100.000đ, thay bình báo giá theo loại bình", "20 – 40 phút", null, 8, false),
        Item("kiem-tra-dong-co", "Kiểm tra động cơ", "Sửa chữa", "engine",
            "Kiểm tra các dấu hiệu máy yếu, khó nổ, hao xăng hoặc tiếng máy bất thường.",
            ["Nghe và kiểm tra tình trạng vận hành.", "Kiểm tra bugi, lọc gió, nhớt cơ bản.", "Tư vấn bước sửa chữa tiếp theo nếu cần tháo kiểm tra sâu."],
            "Từ 100.000đ – 300.000đ, sửa chữa phát sinh sẽ báo giá riêng", "30 – 60 phút", null, 9, false),
        Item("ve-sinh-kim-phun-buong-dot", "Vệ sinh kim phun / buồng đốt", "Sửa chữa", "inject",
            "Hỗ trợ xe vận hành mượt hơn khi có dấu hiệu hụt ga, hao xăng hoặc máy không đều.",
            ["Kiểm tra tình trạng vận hành.", "Vệ sinh theo quy trình phù hợp.", "Tư vấn thêm nếu phát hiện lỗi liên quan."],
            "Từ 150.000đ – 350.000đ", "30 – 60 phút", null, 10, false),
        Item("kiem-tra-day-curoa-noi-xe-ga", "Kiểm tra dây curoa / nồi xe ga", "Xe tay ga", "belt",
            "Kiểm tra bộ truyền động xe tay ga khi xe ì, rung đầu hoặc lên ga không mượt.",
            ["Kiểm tra dây curoa.", "Kiểm tra nồi trước/nồi sau cơ bản.", "Tư vấn vệ sinh hoặc thay thế nếu cần."],
            "Kiểm tra từ 100.000đ – 250.000đ, phụ tùng báo giá riêng", "30 – 75 phút", null, 11, false),
        Item("sua-chua-tong-quat", "Sửa chữa tổng quát", "Sửa chữa", "repair",
            "Tiếp nhận các lỗi phát sinh như xe khó nổ, chết máy, tiếng kêu lạ, hao xăng, rung giật.",
            ["Tiếp nhận tình trạng xe.", "Kiểm tra ban đầu.", "Báo lỗi dự kiến và chi phí trước khi sửa."],
            "Kiểm tra từ 0đ – 100.000đ, sửa chữa báo giá sau kiểm tra", "Tùy tình trạng xe", null, 12, false),
        Item("thay-phu-tung-chinh-hang", "Thay phụ tùng chính hãng", "Phụ tùng", "parts",
            "Tư vấn và thay thế phụ tùng phù hợp với từng dòng xe.",
            ["Kiểm tra phụ tùng cần thay.", "Tư vấn phụ tùng phù hợp.", "Báo giá trước khi thay."],
            "Báo giá theo phụ tùng thực tế", "Tùy phụ tùng", null, 13, false),
        Item("kiem-tra-xe-truoc-chuyen-di", "Kiểm tra xe trước chuyến đi", "An toàn", "trip",
            "Kiểm tra nhanh các hạng mục quan trọng trước khi đi xa.",
            ["Kiểm tra phanh, lốp, đèn, còi.", "Kiểm tra nhớt và rò rỉ cơ bản.", "Tư vấn xử lý các hạng mục rủi ro."],
            "Từ 100.000đ – 200.000đ", "20 – 40 phút", null, 14, false)
    ];

    private static async Task SeedFinanceAsync(HieuNgaDbContext context)
    {
        var bankType = new BankType
        {
            Name = "Ngân hàng thương mại",
            Slug = "ngan-hang-thuong-mai",
            DisplayOrder = 1,
            IsActive = true
        };
        context.BankTypes.Add(bankType);
        await context.SaveChangesAsync();

        var banks = new[]
        {
            ("mb", "MB", "MB Bank", "#0054A6", 1.4m, "Ưu đãi HEAD", true),
            ("tpb", "TP", "TPBank", "#6B2C91", 1.5m, "Duyệt nhanh", false),
            ("agri", "AG", "Agribank", "#006B3F", 1.2m, "Lãi suất thấp", false),
            ("tcb", "TC", "Techcombank", "#E30613", 1.6m, "Phổ biến", false)
        };

        var order = 0;
        foreach (var (id, shortName, name, color, rate, trust, isDefault) in banks)
        {
            var bank = new Bank
            {
                BankTypeId = bankType.Id,
                Name = name,
                ShortName = shortName,
                BrandColor = color,
                DisplayOrder = order++,
                IsActive = true
            };
            context.Banks.Add(bank);
            await context.SaveChangesAsync();

            context.FinanceRates.Add(new FinanceRate
            {
                BankId = bank.Id,
                PlanName = "Trả góp tiêu chuẩn",
                MonthlyInterestRatePercent = rate,
                MinDownPaymentPercent = 0,
                MaxDownPaymentPercent = 70,
                MinTermMonths = 6,
                MaxTermMonths = 36,
                SupportedTermsMonths = "6,12,18,24,36",
                TrustLabel = trust,
                IsDefault = isDefault,
                IsActive = true
            });
        }
    }

    private static async Task SeedSiteSettingDefaultsAsync(HieuNgaDbContext context, ILogger logger)
    {
        var defaults = new Dictionary<string, (string Value, string Group)>
        {
            ["service.pricing_disclaimer"] = (
                "Giá có thể thay đổi theo dòng xe, tình trạng xe và phụ tùng thực tế. Honda Hiếu Nga sẽ kiểm tra và báo giá rõ ràng trước khi thực hiện.",
                "service"),
            ["site.zalo"] = ("https://zalo.me/0905123456", "site"),
            ["site.hours"] = ("T2–T7: 8:00–18:00 · CN: 8:00–17:00", "site"),
            ["seo.default_title"] = ("Honda Hiếu Nga Đà Nẵng | Mua xe & dịch vụ HEAD", "seo"),
            ["seo.default_description"] = ("Đại lý Honda HEAD chính hãng tại Đà Nẵng.", "seo"),
            ["site.footer_text"] = ("HEAD Dealer chính hãng Honda Việt Nam", "site")
        };

        foreach (var (key, (value, group)) in defaults)
        {
            if (await context.SiteSettings.AnyAsync(s => s.Key == key)) continue;
            context.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = group });
            logger.LogDebug("Seeded site setting {Key}", key);
        }
    }

    private static ServiceSeedDef Item(string slug, string name, string cat, string icon, string desc,
        string[] includes, string price, string duration, string? note, int order, bool featured) =>
        new(slug, name, cat, icon, desc, includes, price, duration, note, order, featured);

    private record ServiceSeedDef(
        string Slug, string Name, string CategoryName, string IconKey, string ShortDescription,
        string[] Includes, string Price, string Duration, string? PriceNote, int Order, bool Featured);
}
