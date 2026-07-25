using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain;
using HieuNga.Domain.Enums;
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
        var banners = await bannerRepo.GetByPositionAsync(BannerPosition.Hero, ct);
        var motorcycles = await motorcycleRepo.GetFeaturedAsync(6, ct);
        var promotions = await promotionRepo.GetActiveAsync(6, ct);
        var branches = await branchRepo.GetActiveAsync(ct);
        var reviews = await reviewRepo.GetFeaturedAsync(8, ct);
        var posts = await blogService.GetPublishedAsync(1, 4, null, ct);
        var banks = await financeConfig.GetActiveBanksAsync(ct);
        var services = await serviceCatalog.GetActiveItemsAsync(ct);
        var categoryCounts = await motorcycleRepo.GetPublishedCategoryCountsAsync(ct);
        var categoryThumbs = await motorcycleRepo.GetPublishedCategoryThumbnailsAsync(ct);

        var categories = MotorcycleCategoryLabels.All
            .Select(c => new MotorcycleCategoryCountDto(
                c.Value,
                c.Label,
                categoryCounts.GetValueOrDefault(c.Value),
                categoryThumbs.GetValueOrDefault(c.Value)))
            .ToList();

        return new HomepageDto(
            banners.Select(b => b.ToDto()).ToList(),
            motorcycles.Select(m => m.ToListItem()).ToList(),
            promotions.Select(p => p.ToDto()).ToList(),
            branches.Select(b => b.ToDto()).ToList(),
            reviews.Select(r => r.ToDto(r.Motorcycle?.Name)).ToList(),
            categories,
            posts.Items,
            banks,
            services.Take(6).ToList());
    }
}
