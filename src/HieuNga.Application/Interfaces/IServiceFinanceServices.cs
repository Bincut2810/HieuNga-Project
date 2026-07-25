using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceItemListDto>> GetActiveItemsAsync(CancellationToken ct = default);
    /// <summary>Premium public experience cards (featured first, max count).</summary>
    Task<IReadOnlyList<ServiceItemListDto>> GetExperienceServicesAsync(int count = 6, CancellationToken ct = default);
    Task<ServiceItemDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceItemListDto>> GetRelatedAsync(string slug, int count = 3, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetBookingServiceNamesAsync(CancellationToken ct = default);
    string PricingDisclaimer { get; }
}

public interface IFinanceConfigService
{
    Task<IReadOnlyList<FinanceBankDto>> GetActiveBanksAsync(CancellationToken ct = default);
    Task<FinanceBankDto?> GetDefaultBankAsync(CancellationToken ct = default);
}

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(SiteSettingsDto settings, CancellationToken ct = default);
}
