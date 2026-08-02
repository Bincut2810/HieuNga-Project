namespace HieuNga.Application;

/// <summary>
/// Lead attribution stored in existing Notes fields (no DB migration).
/// Format: [lead source=… intent=… xe=… service=…] human message
/// </summary>
public static class LeadAttribution
{
    public const string TagPrefix = "[lead ";

    public static string BuildNotes(
        string? source,
        string? intent = null,
        string? xe = null,
        string? service = null,
        string? subject = null,
        string? message = null,
        string? extra = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(source)) parts.Add($"source={Sanitize(source)}");
        if (!string.IsNullOrWhiteSpace(intent)) parts.Add($"intent={Sanitize(intent)}");
        if (!string.IsNullOrWhiteSpace(xe)) parts.Add($"xe={Sanitize(xe)}");
        if (!string.IsNullOrWhiteSpace(service)) parts.Add($"service={Sanitize(service)}");
        if (!string.IsNullOrWhiteSpace(extra)) parts.Add(Sanitize(extra));

        var tag = parts.Count > 0 ? $"{TagPrefix}{string.Join(" ", parts)}]" : null;
        var body = string.Join(" — ", new[] { subject, message }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (tag is null) return body;
        if (string.IsNullOrWhiteSpace(body)) return tag;
        return $"{tag} {body}";
    }

    public static string? ExtractSourceLabel(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith(TagPrefix, StringComparison.Ordinal))
            return null;

        var end = notes.IndexOf(']');
        if (end < 0) return null;
        var inner = notes[TagPrefix.Length..end];
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in inner.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = token.IndexOf('=');
            if (eq <= 0) continue;
            map[token[..eq]] = token[(eq + 1)..];
        }

        map.TryGetValue("source", out var source);
        map.TryGetValue("intent", out var intent);
        map.TryGetValue("xe", out var xe);
        map.TryGetValue("service", out var service);

        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(source)) bits.Add(PrettySource(source));
        if (!string.IsNullOrWhiteSpace(intent)) bits.Add($"intent:{intent}");
        if (!string.IsNullOrWhiteSpace(xe)) bits.Add($"xe:{xe}");
        if (!string.IsNullOrWhiteSpace(service)) bits.Add($"dv:{service}");
        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    /// <summary>Human-readable note without the [lead …] tag.</summary>
    public static string? StripTag(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        if (!notes.StartsWith(TagPrefix, StringComparison.Ordinal))
            return notes.Trim();

        var end = notes.IndexOf(']');
        if (end < 0) return notes.Trim();
        var rest = notes[(end + 1)..].Trim();
        return string.IsNullOrWhiteSpace(rest) ? null : rest;
    }

    public static string PrettySource(string source) => source.ToLowerInvariant() switch
    {
        "homepage" or "home" => "Trang chủ",
        "listing" or "catalog" or "xe" => "Danh mục xe",
        "detail" => "Chi tiết xe",
        "promotion" or "khuyen-mai" => "Khuyến mãi",
        "service" or "bao-duong" => "Dịch vụ",
        "finance" or "tra-gop" => "Trả góp",
        "news" or "tin-tuc" => "Tin tức",
        "compare" or "so-sanh" => "So sánh",
        "contact" or "lien-he" or "contactpage" => "Liên hệ",
        "test-ride" or "lai-thu" => "Lái thử",
        _ => source
    };

    private static string Sanitize(string value) =>
        value.Trim().Replace(']', ' ').Replace('[', ' ').Replace('\n', ' ').Replace('\r', ' ');
}
