using HieuNga.Web.Pages.Admin.Xe;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// Unit tests for <see cref="EditorModelStateIsolation"/>.
///
/// Razor Pages binds + validates every <c>[BindProperty]</c> on a PageModel
/// for every POST, so the Motorcycle Editor's nested <c>VariantForm</c> /
/// <c>NewColor</c> / <c>NewFeature</c> / <c>NewTech</c> inputs produced
/// cross-tab <c>[Required]</c> failures on <c>OnPostSaveGeneralAsync</c>
/// before the General handler reached <c>SaveCoreAsync</c>.
///
/// Each handler now calls <c>EditorModelStateIsolation.RemoveFor</c> at the
/// top to strip the prefixes it does not consume. These tests pin the
/// expected behavior so the production bug cannot regress.
/// </summary>
public class EditorModelStateIsolationTests
{
    [Fact]
    public void RemoveFor_StripsExactAndNestedKeys_WhileKeepingOthers()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("Input.BasePrice", "out of range");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("VariantForm.Price", "negative");
        ms.AddModelError("VariantForm", "container error");
        ms.AddModelError("NewColor.Name", "required");
        ms.AddModelError("NewColor.HexCode", "required");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("NewTech.Title", "The Title field is required.");
        ms.AddModelError("PublishStatus", "bad value");
        ms.AddModelError("ThumbnailFile", "too big");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "NewFeature", "NewTech");

        // Kept
        Assert.True(ms.ContainsKey("Input.Name"));
        Assert.True(ms.ContainsKey("Input.BasePrice"));
        Assert.True(ms.ContainsKey("PublishStatus"));
        Assert.True(ms.ContainsKey("ThumbnailFile"));

        // Stripped
        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("VariantForm.Price"));
        Assert.False(ms.ContainsKey("VariantForm"));
        Assert.False(ms.ContainsKey("NewColor.Name"));
        Assert.False(ms.ContainsKey("NewColor.HexCode"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
        Assert.False(ms.ContainsKey("NewTech.Title"));
    }

    [Fact]
    public void RemoveFor_NoPrefixes_IsNoOp()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "x");
        ms.AddModelError("VariantForm.Name", "x");
        ms.AddModelError("NewFeature.Title", "x");

        EditorModelStateIsolation.RemoveFor(ms);

        Assert.True(ms.ContainsKey("Input.Name"));
        Assert.True(ms.ContainsKey("VariantForm.Name"));
        Assert.True(ms.ContainsKey("NewFeature.Title"));
    }

    [Fact]
    public void RemoveFor_NullPrefixes_IsNoOp()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "x");
        ms.AddModelError("VariantForm.Name", "x");

        EditorModelStateIsolation.RemoveFor(ms, null!);

        Assert.True(ms.ContainsKey("Input.Name"));
        Assert.True(ms.ContainsKey("VariantForm.Name"));
    }

    [Fact]
    public void RemoveFor_DoesNotStripNamesThatHappenToContainThePrefix()
    {
        // Ensure the prefix matcher is anchored to "." boundary.
        var ms = new ModelStateDictionary();
        ms.AddModelError("VariantForm.Name", "x");
        ms.AddModelError("VariantForm2.Name", "x");
        ms.AddModelError("Input.VariantForm.Name", "x");

        EditorModelStateIsolation.RemoveFor(ms, "VariantForm");

        Assert.False(ms.ContainsKey("VariantForm.Name"));
        // "VariantForm2" is NOT a child of "VariantForm"; only the exact match
        // and "VariantForm.*" should be removed.
        Assert.True(ms.ContainsKey("VariantForm2.Name"));
        // "Input.VariantForm.Name" is nested under Input, not under VariantForm.
        Assert.True(ms.ContainsKey("Input.VariantForm.Name"));
    }

    [Fact]
    public void RemoveFor_SaveGeneralProfile_YieldsValidModelStateWhenOnlyInputIsValid()
    {
        // Simulates the post-binding ModelState for a Create-General POST:
        // the user supplied Input.* fields but the nested BindProperty
        // objects are default-constructed and empty.
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe"); // intentional — would still be invalid
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("NewTech.Title", "The Title field is required.");

        // Now switch to a clean scenario: Input is valid, nested models aren't.
        var ms2 = new ModelStateDictionary();
        ms2.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms2.AddModelError("NewFeature.Title", "The Title field is required.");
        ms2.AddModelError("NewTech.Title", "The Title field is required.");
        ms2.AddModelError("NewColor.Name", "required");

        EditorModelStateIsolation.RemoveFor(ms2,
            "VariantForm", "NewColor", "NewFeature", "NewTech");

        Assert.True(ms2.IsValid);
        Assert.Empty(ms2);
    }

    [Fact]
    public void RemoveFor_SaveGeneralProfile_StillInvalidWhenInputNameIsInvalid()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "NewFeature", "NewTech");

        Assert.False(ms.IsValid);
        Assert.True(ms.ContainsKey("Input.Name"));
        Assert.False(ms.ContainsKey("VariantForm.Name"));
    }

    [Fact]
    public void RemoveFor_SaveGeneralProfile_StillInvalidWhenInputBasePriceIsOutOfRange()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.BasePrice", "The field BasePrice must be between 0 and 1.7976931348623157E+308.");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "NewFeature", "NewTech");

        Assert.False(ms.IsValid);
        Assert.True(ms.ContainsKey("Input.BasePrice"));
    }

    [Fact]
    public void RemoveFor_SaveGeneralProfile_StillInvalidWhenInputCategoryIsMissing()
    {
        // MotorcycleInputModel.Category has [Required]. When 0 (default enum
        // value) is bound without a posted selection, MVC records a required
        // failure on Input.Category.
        var ms = new ModelStateDictionary();
        ms.AddModelError("Input.Category", "The Category field is required.");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "NewFeature", "NewTech");

        Assert.False(ms.IsValid);
        Assert.True(ms.ContainsKey("Input.Category"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
    }

    [Fact]
    public void RemoveFor_AddFeatureProfile_KeepsNewFeatureRequiredErrors()
    {
        // AddFeature handler strips everything except NewFeature.*, so an
        // empty Title must remain in ModelState (the handler reads
        // NewFeature.Title directly and the controller layer can detect it).
        var ms = new ModelStateDictionary();
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewTech.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "Input", "NewTech");

        Assert.True(ms.ContainsKey("NewFeature.Title"));
        Assert.False(ms.ContainsKey("Input.Name"));
        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("NewTech.Title"));
    }

    [Fact]
    public void RemoveFor_AddTechProfile_KeepsNewTechRequiredErrors()
    {
        var ms = new ModelStateDictionary();
        ms.AddModelError("NewTech.Title", "The Title field is required.");
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "NewColor", "NewFeature", "Input");

        Assert.True(ms.ContainsKey("NewTech.Title"));
        Assert.False(ms.ContainsKey("Input.Name"));
        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
    }

    [Fact]
    public void RemoveFor_AddColorProfile_KeepsNewColorRequiredErrors()
    {
        // The current codebase does not host OnPostAddColor on EditorModel
        // (colors are edited elsewhere), but the prefix filter must behave
        // correctly if/when such a handler is added.
        var ms = new ModelStateDictionary();
        ms.AddModelError("NewColor.Name", "The Name field is required.");
        ms.AddModelError("NewColor.HexCode", "The HexCode field is required.");
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("NewTech.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "VariantForm", "Input", "NewFeature", "NewTech");

        Assert.True(ms.ContainsKey("NewColor.Name"));
        Assert.True(ms.ContainsKey("NewColor.HexCode"));
        Assert.False(ms.ContainsKey("Input.Name"));
        Assert.False(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
        Assert.False(ms.ContainsKey("NewTech.Title"));
    }

    [Fact]
    public void RemoveFor_AddVariantProfile_KeepsVariantFormRequiredErrors()
    {
        // Same: no OnPostAddVariant handler on EditorModel today, but the
        // filter must keep VariantForm.* errors when that's the active prefix.
        var ms = new ModelStateDictionary();
        ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
        ms.AddModelError("Input.Name", "Vui lòng nhập tên xe");
        ms.AddModelError("NewColor.Name", "required");
        ms.AddModelError("NewFeature.Title", "The Title field is required.");
        ms.AddModelError("NewTech.Title", "The Title field is required.");

        EditorModelStateIsolation.RemoveFor(ms,
            "Input", "NewColor", "NewFeature", "NewTech");

        Assert.True(ms.ContainsKey("VariantForm.Name"));
        Assert.False(ms.ContainsKey("Input.Name"));
        Assert.False(ms.ContainsKey("NewColor.Name"));
        Assert.False(ms.ContainsKey("NewFeature.Title"));
        Assert.False(ms.ContainsKey("NewTech.Title"));
    }

    [Fact]
    public void RemoveFor_NullModelState_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => EditorModelStateIsolation.RemoveFor(null!, "VariantForm"));
    }
}
