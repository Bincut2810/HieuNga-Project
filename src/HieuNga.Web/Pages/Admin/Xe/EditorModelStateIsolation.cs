using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>
/// Small ModelState utility used by <see cref="EditorModel"/> to strip
/// validation entries belonging to unrelated <c>[BindProperty]</c> nested
/// inputs.
///
/// Background: Razor Pages' <c>PageModelBinder</c> treats every
/// <c>[BindProperty]</c> on a PageModel as a top-level form parameter, so
/// validation runs on all of them on every POST. The Motorcycle Editor
/// declares <c>Input</c>, <c>VariantForm</c>, <c>NewFeature</c> and
/// <c>NewTech</c> as <c>[BindProperty]</c>, each hosting classes with
/// <c>[Required]</c> string fields. When a tab-specific form is submitted,
/// the unrelated nested models stay at their default <c>string.Empty</c>
/// and fail validation before the handler runs.
///
/// Two helpers are exposed:
///
/// <list type="bullet">
///   <item><see cref="RemoveFor"/> — strip by *prefix* (used when the
///   handler knows the exact nested-model prefixes it doesn't consume).</item>
///   <item><see cref="RemoveAllExcept"/> — strip everything *not* matching
///   the whitelist of prefixes the handler actually consumes. This is the
///   robust fix for the production SaveGeneral failure where the binding
///   pipeline emits unprefixed nested keys (e.g. <c>"Name"</c>,
///   <c>"Title"</c>) that the prefix-strip approach cannot reach.</item>
/// </list>
/// </summary>
public static class EditorModelStateIsolation
{
    /// <summary>
    /// Removes ModelState entries whose key matches or starts with
    /// <c>prefix + "."</c> for any of the supplied <paramref name="prefixes"/>.
    /// No-op when <paramref name="prefixes"/> is null or empty.
    /// </summary>
    public static void RemoveFor(ModelStateDictionary modelState, params string[] prefixes)
    {
        if (modelState is null) throw new ArgumentNullException(nameof(modelState));
        if (prefixes is null || prefixes.Length == 0) return;

        // Snapshot keys before mutating the dictionary.
        var keys = modelState.Keys.ToList();
        foreach (var key in keys)
        {
            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrEmpty(prefix)) continue;
                if (string.Equals(key, prefix, StringComparison.Ordinal) ||
                    key.StartsWith(prefix + ".", StringComparison.Ordinal))
                {
                    modelState.Remove(key);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Removes every ModelState entry whose key is NOT equal to, and does
    /// not start with <c>prefix + "."</c> for any of the supplied
    /// <paramref name="keepPrefixes"/>. The handler whitelist (the set of
    /// top-level <c>[BindProperty]</c> properties it actually consumes)
    /// drives the cut.
    ///
    /// Why this exists: the prefix-strip approach in <see cref="RemoveFor"/>
    /// fails when ASP.NET Core emits ModelState keys WITHOUT the nested
    /// property prefix (observed at runtime as <c>"Name"</c> and
    /// <c>"Title"</c> failing validation during General Create). Using a
    /// whitelist avoids assuming any specific key shape.
    /// </summary>
    public static void RemoveAllExcept(ModelStateDictionary modelState, params string[] keepPrefixes)
    {
        if (modelState is null) throw new ArgumentNullException(nameof(modelState));
        if (keepPrefixes is null || keepPrefixes.Length == 0)
        {
            // No whitelist → drop everything (no-op guard preserves caller intent).
            var allKeys = modelState.Keys.ToList();
            foreach (var k in allKeys) modelState.Remove(k);
            return;
        }

        var keys = modelState.Keys.ToList();
        foreach (var key in keys)
        {
            var keep = false;
            foreach (var prefix in keepPrefixes)
            {
                if (string.IsNullOrEmpty(prefix)) continue;
                if (string.Equals(key, prefix, StringComparison.Ordinal) ||
                    key.StartsWith(prefix + ".", StringComparison.Ordinal))
                {
                    keep = true;
                    break;
                }
            }
            if (!keep) modelState.Remove(key);
        }
    }
}
