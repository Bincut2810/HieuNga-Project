using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// Tests for the AdminUi TempData helpers (success / error flashes).
///
/// Phase 2 — set/peek semantics:
///   - SetSuccess must drop any stale error so the previous failure's
///     banner cannot re-render alongside the new success.
///   - SetError must drop any stale success for the same lifecycle.
///   - PeekSuccess / PeekError must NOT consume the entry (the entry
///     stays available for the _AdminFlash partial to render).
///   - The indexes used are AdminSuccess / AdminError (constant string
///     keys), not random, so external code can read them.
/// </summary>
public class AdminToastTempDataTests
{
    [Fact]
    public void SetSuccess_DropsStaleError()
    {
        var page = NewPage();
        page.TempData["AdminError"] = "error cũ";

        page.SetSuccess("thành công");

        Assert.Equal("thành công", page.TempData["AdminSuccess"]);
        Assert.False(page.TempData.ContainsKey("AdminError"),
            "SetSuccess must Remove(ErrorKey) so the stale banner doesn't re-render.");
    }

    [Fact]
    public void SetError_DropsStaleSuccess()
    {
        var page = NewPage();
        page.TempData["AdminSuccess"] = "thành công cũ";

        page.SetError("đã xảy ra lỗi");

        Assert.Equal("đã xảy ra lỗi", page.TempData["AdminError"]);
        Assert.False(page.TempData.ContainsKey("AdminSuccess"));
    }

    [Fact]
    public void PeekSuccess_DoesNotConsume()
    {
        var page = NewPage();
        page.TempData["AdminSuccess"] = "stays";

        Assert.Equal("stays", page.PeekSuccess());
        Assert.Equal("stays", page.PeekSuccess());
        Assert.Equal("stays", page.PeekSuccess());
    }

    [Fact]
    public void PeekError_DoesNotConsume()
    {
        var page = NewPage();
        page.TempData["AdminError"] = "boom";

        Assert.Equal("boom", page.PeekError());
        Assert.Equal("boom", page.PeekError());
    }

    [Fact]
    public void PeekSuccess_NoValue_ReturnsNull()
    {
        var page = NewPage();
        Assert.Null(page.PeekSuccess());
        Assert.Null(page.PeekError());
    }

    [Fact]
    public void SetSuccess_OverwriteAndDropError()
    {
        // 1) Error first.
        var page = NewPage();
        page.SetError("Lỗi ban đầu");
        Assert.Equal("Lỗi ban đầu", page.TempData["AdminError"]);

        // 2) Recovery: success overwrites — and MUST drop the stale error
        //    so the two banners cannot co-render.
        page.SetSuccess("Đã thành công");
        Assert.Equal("Đã thành công", page.TempData["AdminSuccess"]);
        Assert.False(page.TempData.ContainsKey("AdminError"));
    }

    [Fact]
    public void Keys_AreStableConstants()
    {
        // External JS / partials rely on these keys being stable.
        Assert.Equal("AdminSuccess", AdminUi.SuccessKey);
        Assert.Equal("AdminError",   AdminUi.ErrorKey);
    }

    [Fact]
    public void SetSuccess_OverwritesExistingSuccess()
    {
        var page = NewPage();
        page.SetSuccess("first");
        page.SetSuccess("second");
        Assert.Equal("second", page.TempData["AdminSuccess"]);
    }

    [Fact]
    public void SetError_OverwritesExistingError()
    {
        var page = NewPage();
        page.SetError("first");
        page.SetError("second");
        Assert.Equal("second", page.TempData["AdminError"]);
    }

    private static Microsoft.AspNetCore.Mvc.RazorPages.PageModel NewPage()
    {
        // Minimal PageModel with an in-memory TempData dictionary.
        var page = new TestPageModel();
        page.TempData = new TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            new InMemoryTempDataProvider());
        return page;
    }

    private sealed class TestPageModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object?> _store = new();

        public IDictionary<string, object?> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) =>
            new Dictionary<string, object?>(_store);

        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object?> values)
        {
            _store.Clear();
            foreach (var kv in values) _store[kv.Key] = kv.Value;
        }
    }
}
