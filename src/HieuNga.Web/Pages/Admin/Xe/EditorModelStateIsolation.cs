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
/// declares <c>Input</c>, <c>VariantForm</c>, <c>NewColor</c>,
/// <c>NewFeature</c> and <c>NewTech</c> as <c>[BindProperty]</c>, each
/// hosting classes with <c>[Required]</c> string fields. When a tab-specific
/// form is submitted, the unrelated nested models stay at their default
/// <c>string.Empty</c> and fail validation before the handler runs.
///
/// This helper removes ModelState entries whose key matches or is nested
/// under any of the supplied prefixes (e.g. <c>"VariantForm"</c> removes
/// both <c>"VariantForm"</c> and <c>"VariantForm.Name"</c>), so each
/// handler can validate only the nested model it actually consumes.
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
}
