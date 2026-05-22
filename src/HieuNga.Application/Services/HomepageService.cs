using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class HomepageService(
    IBannerRepository bannerRepo,
    IMotorcycleRepository motorcycleRepo,
    IPromotionRepository promotionRepo,
    IBranchRepository branchRepo,
    IReviewRepository reviewRepo) : IHomepageService
{
    public async Task<HomepageDto> GetHomepageDataAsync(CancellationToken ct = default)
    {
        var banners = await bannerRepo.GetByPositionAsync(BannerPosition.Hero, ct);
        var motorcycles = await motorcycleRepo.GetFeaturedAsync(6, ct);
        var promotions = await promotionRepo.GetActiveAsync(4, ct);
        var branches = await branchRepo.GetActiveAsync(ct);
        var reviews = await reviewRepo.GetFeaturedAsync(6, ct);

        return new HomepageDto(
            banners.Select(b => b.ToDto()).ToList(),
            motorcycles.Select(m => m.ToListItem()).ToList(),
            promotions.Select(p => p.ToDto()).ToList(),
            branches.Select(b => b.ToDto()).ToList(),
            reviews.Select(r => r.ToDto(r.Motorcycle?.Name)).ToList());
    }
}
