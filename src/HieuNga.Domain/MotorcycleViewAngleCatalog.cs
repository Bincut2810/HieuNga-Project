using HieuNga.Domain.Enums;

namespace HieuNga.Domain;

/// <summary>Canonical six-angle catalog — labels for CMS and public UI.</summary>
public static class MotorcycleViewAngleCatalog
{
    public sealed record Entry(MotorcycleViewAngle Angle, string Key, string LabelVi, string LabelEn);

    public static readonly IReadOnlyList<Entry> All =
    [
        new(MotorcycleViewAngle.Front, "front", "Trước", "Front"),
        new(MotorcycleViewAngle.FrontLeft, "front-left", "Trước trái", "Front Left"),
        new(MotorcycleViewAngle.Left, "left", "Trái", "Left"),
        new(MotorcycleViewAngle.Rear, "rear", "Sau", "Rear"),
        new(MotorcycleViewAngle.Right, "right", "Phải", "Right"),
        new(MotorcycleViewAngle.FrontRight, "front-right", "Trước phải", "Front Right")
    ];

    public const int Count = 6;

    public static Entry Get(MotorcycleViewAngle angle) =>
        All.First(e => e.Angle == angle);

    public static bool TryParseKey(string? keyOrFile, out MotorcycleViewAngle angle)
    {
        angle = default;
        if (string.IsNullOrWhiteSpace(keyOrFile)) return false;
        var raw = Path.GetFileNameWithoutExtension(keyOrFile).Trim().ToLowerInvariant()
            .Replace('_', '-').Replace(' ', '-');

        foreach (var e in All)
        {
            if (raw == e.Key || raw == e.LabelEn.ToLowerInvariant().Replace(' ', '-'))
            {
                angle = e.Angle;
                return true;
            }
        }

        // Vietnamese / shorthand
        angle = raw switch
        {
            "truoc" or "front" or "0" => MotorcycleViewAngle.Front,
            "truoc-trai" or "frontleft" or "fl" or "1" => MotorcycleViewAngle.FrontLeft,
            "trai" or "left" or "2" => MotorcycleViewAngle.Left,
            "sau" or "rear" or "back" or "3" => MotorcycleViewAngle.Rear,
            "phai" or "right" or "4" => MotorcycleViewAngle.Right,
            "truoc-phai" or "frontright" or "fr" or "5" => MotorcycleViewAngle.FrontRight,
            _ => (MotorcycleViewAngle)(-1)
        };
        return (int)angle >= 0;
    }
}
