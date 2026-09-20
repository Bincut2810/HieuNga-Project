using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class HomepageService(
    IBannerRepository bannerRepo,
    IMotorcycleRepository motorcycleRepo,
    IPromotionRepository promotionRepo,
    IBranchRepository branchRepo,
    IReviewRepository reviewRepo,
    IBlogService blogService,
    IFinanceConfigService financeConfig,
    IServiceCatalogService serviceCatalog) : IHomepageService
{
    public async Task<HomepageDto> GetHomepageDataAsync(CancellationToken ct = default)
    {
        var banners = await bannerRepo.GetHomepageBannersAsync(20, ct);
        var motorcycles = await motorcycleRepo.GetFeaturedAsync(6, ct);
        var promotions = await promotionRepo.GetActiveAsync(6, ct);
        var branches = await branchRepo.GetActiveAsync(ct);
        var reviews = await reviewRepo.GetFeaturedAsync(8, ct);
        var posts = await blogService.GetPublishedAsync(1, 4, null, ct);
        var banks = await financeConfig.GetActiveBanksAsync(ct);
        var services = await serviceCatalog.GetExperienceServicesAsync(6, ct);
        var categoryCounts = await motorcycleRepo.GetPublishedCategoryCountsAsync(ct);
        var categoryThumbs = await motorcycleRepo.GetPublishedCategoryThumbnailsAsync(ct);

        var categories = MotorcycleCategoryLabels.All
            .Select(c =>
            {
                var count = categoryCounts.GetValueOrDefault(c.Value);
                var rawThumb = categoryThumbs.GetValueOrDefault(c.Value);
                var image = MotorcycleImageCatalog.IsValidImageUrl(rawThumb) ? rawThumb : null;
                return new MotorcycleCategoryCountDto(c.Value, c.Label, count, image);
            })
            .ToList();

        return new HomepageDto(
            banners.ToHomepageHero(),
            motorcycles.Select(m => m.ToListItem()).ToList(),
            promotions.Select(p => p.ToDto()).ToList(),
            branches.Select(b => b.ToDto()).ToList(),
            VerifiedTestimonials,
            categories,
            posts.Items,
            banks,
            services.Take(6).ToList());
    }

    /// <summary>
    /// Verified customer testimonials publicly displayed on the Hiếu Nga website.
    /// Hard-coded for the production homepage so they render exactly as published,
    /// with no fabricated vehicle, branch, date, or external source metadata.
    /// </summary>
    private static readonly IReadOnlyList<ReviewDto> VerifiedTestimonials = new[]
    {
        new ReviewDto(
            Guid.Parse("11111111-1111-1111-1111-111111111101"),
            "Nguyễn Thanh Hương",
            5,
            null,
            "Công ty Honda Hiếu Nga hậu mãi khách hàng tốt, luôn luôn quan tâm đến chiếc xe mà công ty đã bán ra, thường xuyên lắng nghe, trao đổi tận tình ý kiến KH",
            null,
            null),
        new ReviewDto(
            Guid.Parse("11111111-1111-1111-1111-111111111102"),
            "Nguyễn Xuân Trường",
            5,
            null,
            "Phục vụ rất chuyên nghiệp và nhiệt tình giá cả rẻ hơn các cửa hàng khác cho 5 sao",
            null,
            null)
    };
}
