using HieuNga.Domain.Enums;

namespace HieuNga.Domain;

public static class MotorcycleCategoryLabels
{
    public static string ToDisplayName(this MotorcycleCategory category) => category switch
    {
        MotorcycleCategory.Scooter => "Scooter",
        MotorcycleCategory.XeSo => "Xe số",
        MotorcycleCategory.ConTay => "Xe côn tay",
        MotorcycleCategory.PhanKhoiLon => "Xe phân khối lớn",
        MotorcycleCategory.Electric => "Xe điện",
        _ => "Scooter"
    };

    public static IReadOnlyList<(MotorcycleCategory Value, string Label)> All { get; } =
    [
        (MotorcycleCategory.Scooter, "Scooter"),
        (MotorcycleCategory.XeSo, "Xe số"),
        (MotorcycleCategory.ConTay, "Xe côn tay"),
        (MotorcycleCategory.PhanKhoiLon, "Xe phân khối lớn"),
        (MotorcycleCategory.Electric, "Xe điện")
    ];
}
