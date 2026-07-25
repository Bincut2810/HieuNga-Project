using HieuNga.Domain.Enums;

namespace HieuNga.Application.Catalog;

/// <summary>Sprint 3.6.1 — published inventory targets for the five public categories.</summary>
public static class HieuNgaInventoryTargets
{
    public static IReadOnlyDictionary<MotorcycleCategory, int> Targets { get; } =
        new Dictionary<MotorcycleCategory, int>
        {
            [MotorcycleCategory.Scooter] = 6,      // Xe tay ga
            [MotorcycleCategory.XeSo] = 4,
            [MotorcycleCategory.ConTay] = 4,
            [MotorcycleCategory.PhanKhoiLon] = 4,
            [MotorcycleCategory.Electric] = 3
        };

    public static string CategoryThumb(MotorcycleCategory category) => category switch
    {
        MotorcycleCategory.Scooter => "/images/motorcycles/honda-vision-2025.svg",
        MotorcycleCategory.XeSo => "/images/motorcycles/default.svg",
        MotorcycleCategory.ConTay => "/images/motorcycles/honda-winner-x.svg",
        MotorcycleCategory.PhanKhoiLon => "/images/motorcycles/honda-cb150r.svg",
        MotorcycleCategory.Electric => "/images/motorcycles/default.svg",
        _ => "/images/motorcycles/default.svg"
    };
}
