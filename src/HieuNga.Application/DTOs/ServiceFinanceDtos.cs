namespace HieuNga.Application.DTOs;

public record ServiceCategoryDto(Guid Id, string Name, string Slug);

public record ServiceItemListDto(
    string Slug,
    string Name,
    string Category,
    string IconKey,
    string EstimatedPrice,
    string? EstimatedDuration,
    string? ThumbnailUrl = null,
    string? ShortDescription = null,
    bool IsFeatured = false);

public record ServiceFaqDto(string Question, string Answer);

public record ServiceItemDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string Category,
    string IconKey,
    string ShortDescription,
    string? DetailDescription,
    IReadOnlyList<string> Includes,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> ProcessSteps,
    IReadOnlyList<string> Gallery,
    IReadOnlyList<ServiceFaqDto> Faqs,
    string? ThumbnailUrl,
    string? HeroImageUrl,
    string EstimatedPrice,
    string? EstimatedDuration,
    string? PriceNote,
    SeoMetadataDto? Seo);

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
