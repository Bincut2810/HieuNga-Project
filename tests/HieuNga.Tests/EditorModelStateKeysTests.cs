using HieuNga.Web.Pages.Admin.Xe;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// The runtime evidence shows that <c>OnPostSaveGeneralAsync</c> sees
/// <c>ModelState</c> entries with unprefixed keys (<c>Name</c>, <c>Title</c>,
/// …) carrying the cross-tab <c>[Required]</c> messages from the nested
/// <c>VariantForm</c>, <c>NewFeature</c>, <c>NewTech</c>
/// <c>[BindProperty]</c> models. The current <c>RemoveFor(...)</c> helper
/// strips by prefix (<c>VariantForm.Name</c>, etc.) so it misses the actual
/// unprefixed keys and the helper is a no-op in production.
///
/// These tests pin the BOTH shapes (prefixed + unprefixed) so the fix in
/// <see cref="EditorModelStateIsolation"/> works regardless of which shape
/// ASP.NET Core emits today or in future versions.
/// </summary>
public class EditorModelStateKeysTests
{
    /// <summary>
    /// The shape observed in production logs:
    /// <c>Key=Name</c> with error <c>"Vui lòng nhập tên phiên bản"</c>,
    /// <c>Key=Title</c> with <c>"The Title field is required."</c>.
    /// </summary>
    [Fact]
    public void UnprefixedVariantAndFeatureKeys_AreTheShapeSeenAtRuntime()
    {
        var ms = new ModelStateDictionary();
        // Production Key=Name / Error=Vui lòng nhập tên phiên bản
        ms.AddModelError("Name", "Vui lòng nhập tên phiên bản");
        // Production Key=Title / Error=The Title field is required.
        ms.AddModelError("Title", "The Title field is required.");

        // The current RemoveFor helper must NOT handle this case (it strips
        // by prefix, so "Name" / "Title" survive and the handler fails).
        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewFeature", "NewTech");

        Assert.True(ms.ContainsKey("Name"),
            "Bug confirmed: RemoveFor leaves unprefixed 'Name' / 'Title' keys intact.");
        Assert.True(ms.ContainsKey("Title"));
    }

    /// <summary>
    /// The shape that some ASP.NET Core versions produce: <c>VariantForm.Name</c>,
    /// <c>NewFeature.Title</c>, …
    /// </summary>
    [Fact]
    public void PrefixedVariantAndFeatureKeys_AreTheShapeSomeAspNetVersionsProduce()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewFeature", "NewTech");

        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
    }

    /// <summary>
    /// Whatever the runtime emits, the FIXED handler must reach
    /// <c>SaveCoreAsync</c> when <c>Input.*</c> is valid. We simulate the
    /// exact production scenario: a General Create POST with valid
    /// <c>Input</c> values, and the binding pipeline has populated the
    /// nested required-field errors — under whatever key shape.
    /// </summary>
    [Theory]
    [InlineData("Name", "Title")]            // unprefixed (production)
    [InlineData("VariantForm.Name", "NewFeature.Title")] // prefixed (some versions)
    public void FixedHandler_StripsPollutionRegardlessOfKeyShape(string nameKey, string titleKey)
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError(nameKey, "Vui lòng nhập tên phiên bản");
        ms.AddModelError(titleKey, "The Title field is required.");

        // Simulate Input.* valid (no entries for Input.Name etc.).
        // The new fix must result in a ModelState without the cross-tab pollution.
        EditorModelStateIsolation.RemoveAllExcept(ms, "Input", "ThumbnailFile", "PublishStatus", "Tab", "Id");

        Assert.False(ms.ContainsKey(nameKey));
        Assert.False(ms.ContainsKey(titleKey));
    }

    /// <summary>
    /// The whitelist must KEEP <c>Input.*</c> entries (so real validation
    /// failures on the General form still surface) and DROP everything else.
    /// </summary>
    [Fact]
    public void RemoveAllExcept_KeepsInputErrorsAndStripsNestedPollution()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("Input.BasePrice", "out of range");
        ms.AddModelError("Name", "Vui lòng nhập tên phiên bản"); // unprefixed pollution
        ms.AddModelError("Title", "The Title field is required.");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("NewTech.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveAllExcept(ms,
            "Input", "ThumbnailFile", "PublishStatus", "Tab", "Id");

        Assert.True(ms.ContainsKey("Input.Name"));
        Assert.True(ms.ContainsKey("Input.BasePrice"));
        Assert.False(ms.ContainsKey("Name"));
        Assert.False(ms.ContainsKey("Title"));
        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
        Assert.False(ms.ContainsKey("NewTech.Title"));
    }

    [Fact]
    public void RemoveAllExcept_NullModelState_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => EditorModelStateIsolation.RemoveAllExcept(null!, "Input"));
    }

    [Fact]
    public void RemoveAllExcept_NoPrefixes_RemovesEverything()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "x");
        ms.AddModelError("Name", "y");

        EditorModelStateIsolation.RemoveAllExcept(ms);

        Assert.Empty(ms);
    }
}
