using System.Text.RegularExpressions;

namespace HieuNga.Application.Media;

public static class MediaFrameNaming
{
    public static int? TryParseFrameNumber(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name)) return null;
        var match = Regex.Match(name, @"(\d+)(?!.*\d)");
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var n) ? n : null;
    }
}
