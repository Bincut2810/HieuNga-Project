using System.Text;

namespace HieuNga.Application.TestRide;

/// <summary>
/// Normalizes VN mobile numbers so 090… / +8490… / 8490… match for duplicate detection.
/// Canonical storage form: leading 0 (e.g. 0905123456).
/// </summary>
public static class TestRidePhoneNormalizer
{
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new StringBuilder(phone.Length);
        foreach (var c in phone)
        {
            if (char.IsDigit(c))
                digits.Append(c);
        }

        var raw = digits.ToString();
        if (raw.Length == 0)
            return string.Empty;

        if (raw.StartsWith("84", StringComparison.Ordinal) && raw.Length >= 11)
            return "0" + raw[2..];

        return raw;
    }

    /// <summary>Forms that may already exist in the DB for the same customer.</summary>
    public static IReadOnlyList<string> LookupVariants(string? phone)
    {
        var normalized = Normalize(phone);
        if (normalized.Length == 0)
            return [];

        var variants = new HashSet<string>(StringComparer.Ordinal) { normalized };

        if (normalized.StartsWith('0') && normalized.Length >= 10)
        {
            var national = normalized[1..];
            variants.Add("84" + national);
            variants.Add("+84" + national);
        }

        return variants.ToList();
    }
}
