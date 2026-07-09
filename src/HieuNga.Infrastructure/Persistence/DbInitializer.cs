using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

public static class DbInitializer
{
    private const int MaxAttempts = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private const string DevDefaultAdminEmail = "admin@hondahieunga.vn";
    private const string DevDefaultAdminPassword = "Admin@123456!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HieuNgaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HieuNgaDbContext>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var seedOptions = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

        await ExecuteWithRetryAsync(
            async () => await context.Database.MigrateAsync(),
            "Applying database migrations",
            logger);

        var shouldDemoSeed = environment.IsDevelopment() || seedOptions.EnableDemoSeed;

        if (!await context.Motorcycles.AnyAsync())
        {
            if (shouldDemoSeed)
            {
                logger.LogInformation("Seeding initial database...");
                await SeedInitialAsync(context, scope.ServiceProvider, environment, seedOptions, logger);
            }
            else
            {
                logger.LogInformation(
                    "Skipping demo motorcycle seed (set SeedOptions__EnableDemoSeed=true for one-time staging demo).");
            }
        }

        await SeedAdminUserAsync(scope.ServiceProvider, environment, seedOptions, logger);

        if (shouldDemoSeed)
            await SeedDemoContentAsync(context, logger);

        await ServiceFinanceSeed.SeedAsync(context, logger);

        // Content enricher overwrites motorcycle CMS fields — only in Development or when explicitly enabled.
        var runEnricher = environment.IsDevelopment() || seedOptions.RunContentEnricher;
        if (runEnricher)
        {
            await MotorcycleContentEnricher.EnrichAsync(context, logger);
        }
        else
        {
            logger.LogInformation(
                "Skipping motorcycle content enrichment (set SeedOptions:RunContentEnricher=true or use Development to enable).");
        }
    }

    private static async Task ExecuteWithRetryAsync(Func<Task> action, string description, ILogger logger)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransientDbError(ex))
            {
                logger.LogWarning(ex, "{Description} failed (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    description, attempt, MaxAttempts, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay);
            }
        }
    }

    private static bool IsTransientDbError(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
               || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Exception while reading from stream", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SeedInitialAsync(
        HieuNgaDbContext context,
        IServiceProvider sp,
        IHostEnvironment environment,
        SeedOptions seedOptions,
        ILogger logger)
    {

        var branch = new Branch
        {
            Name = "Honda Hiếu Nga HEAD - Đà Nẵng",
            Slug = "honda-hieu-nga-da-nang",
            Address = "123 Nguyễn Văn Linh, Quận 7, Đà Nẵng",
            Phone = "0236 123 4567",
            Hotline = "0905 123 456",
            Email = "contact@hondahieunga.vn",
            IsHeadOffice = true,
            IsActive = true,
            OpeningHours = "T2-T7: 8:00 - 18:00 | CN: 8:00 - 17:00"
        };
        context.Branches.Add(branch);

        context.Banners.AddRange(
            new Banner
            {
                Title = "Khám phá Honda tại Đà Nẵng",
                Subtitle = "Trả góp 0% — Lái thử miễn phí",
                ImageUrl = MotorcycleImageCatalog.Default,
                CtaText = "Xem xe ngay",
                CtaUrl = "/xe",
                Position = BannerPosition.Hero,
                SortOrder = 0,
                IsActive = true
            });

        var motorcycles = new[]
        {
            CreateMotorcycle("Honda Vision 2025", "honda-vision-2025", MotorcycleCategory.Scooter, 35_900_000, 110, true, 0),
            CreateMotorcycle("Honda SH 160i", "honda-sh-160i", MotorcycleCategory.Scooter, 78_500_000, 160, true, 1),
            CreateMotorcycle("Honda Winner X", "honda-winner-x", MotorcycleCategory.Sport, 46_500_000, 150, true, 2),
            CreateMotorcycle("Honda CB150R", "honda-cb150r", MotorcycleCategory.Naked, 52_000_000, 150, false, 3),
        };
        context.Motorcycles.AddRange(motorcycles);

        context.Promotions.Add(new Promotion
        {
            Title = "Ưu đãi trả góp 0% lãi suất",
            Slug = "tra-gop-0-lai-suat",
            Summary = "Hỗ trợ trả góp linh hoạt, thủ tục nhanh chóng",
            Type = PromotionType.Financing,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddMonths(3),
            IsActive = true,
            IsFeatured = true
        });

        context.Reviews.AddRange(
            new Review { CustomerName = "Nguyễn Văn A", Rating = 5, Title = "Dịch vụ tuyệt vời", Content = "Nhân viên tư vấn nhiệt tình, giao xe đúng hẹn.", IsApproved = true, IsFeatured = true, Motorcycle = motorcycles[1] },
            new Review { CustomerName = "Trần Thị B", Rating = 5, Content = "Showroom đẹp, xe chính hãng Honda.", IsApproved = true, IsFeatured = true, Motorcycle = motorcycles[0] }
        );

        context.SiteSettings.AddRange(
            new SiteSetting { Key = "site.name", Value = "Honda Hiếu Nga Đà Nẵng", Group = "general" },
            new SiteSetting { Key = "site.phone", Value = "0905 123 456", Group = "contact" },
            new SiteSetting { Key = "site.address", Value = "123 Nguyễn Văn Linh, Đà Nẵng", Group = "contact" }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Initial seed completed.");
    }

    private static async Task SeedAdminUserAsync(
        IServiceProvider sp,
        IHostEnvironment environment,
        SeedOptions seedOptions,
        ILogger logger)
    {
        if (!environment.IsDevelopment() && !seedOptions.AdminSeedEnabled)
        {
            logger.LogInformation(
                "Admin seed skipped: set SeedOptions__AdminSeedEnabled=true (or AdminSeed__Enabled=true) with email/password on first deploy.");
            return;
        }

        var email = !string.IsNullOrWhiteSpace(seedOptions.AdminEmail)
            ? seedOptions.AdminEmail.Trim()
            : environment.IsDevelopment() ? DevDefaultAdminEmail : null;

        var password = !string.IsNullOrWhiteSpace(seedOptions.AdminPassword)
            ? seedOptions.AdminPassword
            : environment.IsDevelopment() ? DevDefaultAdminPassword : null;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Admin seed skipped: set SeedOptions__AdminEmail and SeedOptions__AdminPassword (12+ chars) before first production deploy.");
            }
            return;
        }

        if (!environment.IsDevelopment() && password.Length < 12)
        {
            logger.LogWarning("Admin seed skipped: production admin password must be at least 12 characters.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var result = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Administrator",
            EmailConfirmed = true
        }, password);

        if (result.Succeeded)
            logger.LogInformation("Admin user seeded for {Email}.", email);
        else
            logger.LogWarning("Admin user seed failed for {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static async Task SeedDemoContentAsync(HieuNgaDbContext context, ILogger logger)
    {
        if (await context.Promotions.CountAsync() >= 3 && await context.BlogPosts.CountAsync() >= 3)
            return;

        logger.LogInformation("Seeding demo content...");

        var motorcycles = await context.Motorcycles.ToListAsync();
        var vision = motorcycles.FirstOrDefault(m => m.Slug == "honda-vision-2025");
        var sh = motorcycles.FirstOrDefault(m => m.Slug == "honda-sh-160i");

        if (await context.Promotions.CountAsync() < 3)
        {
            context.Promotions.AddRange(
                new Promotion
                {
                    Title = "Giảm 2 triệu Honda Vision 2025",
                    Slug = "giam-2-trieu-vision-2025",
                    Summary = "Ưu đãi trực tiếp khi mua xe tại showroom HEAD Đà Nẵng",
                    Content = "<p>Áp dụng cho khách hàng đặt cọc và nhận xe trong tháng. Số lượng có hạn.</p><ul><li>Tặng mũ bảo hiểm Honda</li><li>Miễn phí 3 lần bảo dưỡng đầu</li></ul>",
                    Type = PromotionType.Discount,
                    DiscountAmount = 2_000_000,
                    StartDate = DateTime.UtcNow.AddDays(-14),
                    EndDate = DateTime.UtcNow.AddMonths(2),
                    IsActive = true,
                    IsFeatured = true,
                    MotorcycleId = vision?.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=1200&q=80"
                },
                new Promotion
                {
                    Title = "Tặng phụ kiện trị giá 5 triệu — SH 160i",
                    Slug = "tang-phu-kien-sh-160i",
                    Summary = "Balo, áo mưa, khóa chống trộm cao cấp",
                    Content = "<p>Combo phụ kiện chính hãng dành riêng khách mua SH 160i tại Hiếu Nga.</p>",
                    Type = PromotionType.Gift,
                    StartDate = DateTime.UtcNow.AddDays(-7),
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    MotorcycleId = sh?.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1200&q=80"
                },
                new Promotion
                {
                    Title = "Sự kiện ra mắt xe mới — Đăng ký lái thử",
                    Slug = "su-kien-ra-mat-xe-moi",
                    Summary = "Trải nghiệm dòng xe mới cùng kỹ thuật viên Honda",
                    Content = "<p>Chương trình diễn ra cuối tuần tại showroom. Đăng ký trước để nhận quà lưu niệm.</p>",
                    Type = PromotionType.Event,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1558980664-769d9df238f8?w=1200&q=80"
                });
        }

        if (!await context.BlogCategories.AnyAsync())
        {
            var catNews = new BlogCategory { Name = "Tin tức", Slug = "tin-tuc", SortOrder = 0 };
            var catTips = new BlogCategory { Name = "Mẹo hay", Slug = "meo-hay", SortOrder = 1 };
            context.BlogCategories.AddRange(catNews, catTips);

            context.BlogPosts.AddRange(
                new BlogPost
                {
                    Title = "Honda Vision 2025 chính thức có mặt tại Hiếu Nga Đà Nẵng",
                    Slug = "vision-2025-ra-mat-da-nang",
                    Summary = "Thiết kế trẻ trung, tiết kiệm nhiên liệu, phù hợp di chuyển đô thị.",
                    Content = "<p>Honda Vision 2025 tiếp tục khẳng định vị thế dòng xe tay ga bán chạy nhất phân khúc với động cơ eSP+, tiết kiệm nhiên liệu và bền bỉ.</p><h2>Điểm nổi bật</h2><ul><li>Đèn LED toàn phần</li><li>Cốp rộng 18 lít</li><li>Màu sắc trẻ trung</li></ul>",
                    Category = catNews,
                    AuthorName = "Honda Hiếu Nga",
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    IsPublished = true,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=900&q=80"
                },
                new BlogPost
                {
                    Title = "5 mẹo bảo dưỡng xe tay ga định kỳ tại HEAD",
                    Slug = "5-meo-bao-duong-xe-tay-ga",
                    Summary = "Giữ xe luôn mới, vận hành êm và an toàn với quy trình chuẩn Honda.",
                    Content = "<p>Bảo dưỡng đúng hạn giúp kéo dài tuổi thọ động cơ và giữ giá trị xe khi nâng cấp.</p><ol><li>Thay nhớt đúng km</li><li>Kiểm tra phanh định kỳ</li><li>Vệ sinh lọc gió</li></ol>",
                    Category = catTips,
                    AuthorName = "Kỹ thuật HEAD",
                    PublishedAt = DateTime.UtcNow.AddDays(-5),
                    IsPublished = true,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1625047509168-a7026f36de0c?w=900&q=80"
                },
                new BlogPost
                {
                    Title = "So sánh Honda SH 160i và Winner X: chọn xe nào?",
                    Slug = "so-sanh-sh-160i-winner-x",
                    Summary = "Hai phân khúc khác nhau — tay ga cao cấp vs xe côn thể thao.",
                    Content = "<p>SH 160i hướng tới sự tiện nghi đô thị, Winner X dành cho người yêu thích cảm giác lái thể thao.</p>",
                    Category = catNews,
                    AuthorName = "Tư vấn bán hàng",
                    PublishedAt = DateTime.UtcNow.AddDays(-8),
                    IsPublished = true,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=900&q=80"
                });
        }

        if (!await context.Banners.AnyAsync(b => b.Position == BannerPosition.Promotion))
        {
            context.Banners.Add(new Banner
            {
                Title = "Ưu đãi tháng này",
                Subtitle = "Trả góp 0% — Lái thử miễn phí",
                ImageUrl = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=1400&q=80",
                CtaText = "Xem khuyến mãi",
                CtaUrl = "/khuyen-mai",
                Position = BannerPosition.Promotion,
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }

    private static Motorcycle CreateMotorcycle(string name, string slug, MotorcycleCategory cat, decimal price, int cc, bool featured, int sort) =>
        new()
        {
            Name = name,
            Slug = slug,
            ShortDescription = $"Xe {name} chính hãng Honda",
            Category = cat,
            BasePrice = price,
            EngineCc = cc,
            FuelType = "Xăng",
            IsFeatured = featured,
            IsPublished = true,
            SortOrder = sort,
            ThumbnailUrl = MotorcycleImageCatalog.GetThumbnail(slug),
            OgImageUrl = MotorcycleImageCatalog.GetGalleryPrimary(slug),
            MetaTitle = $"{name} | Honda Hiếu Nga Đà Nẵng",
            MetaDescription = $"Mua {name} chính hãng tại Honda Hiếu Nga HEAD Đà Nẵng. Giá tốt, trả góp 0%, lái thử miễn phí."
        };
}
