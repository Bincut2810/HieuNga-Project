namespace HieuNga.Application.DTOs;

public record ServiceItemListDto(
    Guid Id,
    string Slug,
    string Name,
    string? ShortDescription,
    string? ImageUrl,
    int DisplayOrder);

public record ServiceItemDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? ShortDescription,
    IReadOnlyList<string> Images,
    int DisplayOrder);

public record FinanceBankDto(
    string Id,
    string Name,
    string Initials,
    decimal MonthlyRate,
    decimal RatePercent,
    string RateLabel,
    string? Trust,
    string Color,
    int MinDownPercent,
    int MaxDownPercent,
    IReadOnlyList<int> SupportedTerms,
    bool IsDefault,
    string? LogoUrl = null);

public record SiteSettingsDto(
    string SiteName,
    string Hotline,
    string Phone,
    string ZaloUrl,
    string Email,
    string Address,
    string OpeningHours,
    string DefaultMetaTitle,
    string DefaultMetaDescription,
    string? FacebookUrl,
    string? FooterText,
    string ServicePricingDisclaimer);
