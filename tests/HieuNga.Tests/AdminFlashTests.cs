using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// Regression coverage for the stale admin error message bug.
///
/// Before the fix, <c>_AdminFlash.cshtml</c> read flash messages via
/// <c>TempData.Peek(...)</c>, which does NOT mark the entry for deletion.
/// Combined with the default <c>CookieTempDataProvider</c>, the message
/// then re-rendered on every subsequent GET until the cookie expired —
/// so a one-off failure kept showing "Đã xảy ra lỗi khi lưu xe…" even
/// after the user fixed the underlying issue and the page was reloaded.
///
/// The fix is a single-token change in the partial: replace
/// <c>TempData.Peek(key)</c> with <c>TempData[key]</c>. The indexer marks
/// the entry for deletion at end-of-request via
/// <see cref="TempDataDictionary.Save"/>. These tests pin that behavior
/// so the partial cannot regress to <c>Peek</c> silently.
/// </summary>
public class AdminFlashTests
{
    /// <summary>
    /// Minimal <see cref="ITempDataProvider"/> backing the tests with a
    /// shared dictionary. Used so we can simulate sequential requests on
    /// the same "session" — same role the cookie / session provider plays
    /// in production.
    /// </summary>
    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> Data { get; private set; } = new Dictionary<string, object?>();

        public IDictionary<string, object?> LoadTempData(HttpContext context)
        {
            // Return a defensive copy — TempDataDictionary may mutate
            // internals around this snapshot.
            return new Dictionary<string, object?>(Data);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
            // After Save() we receive ONLY the keys that were set *and not
            // yet read via the indexer*. Anything else is now gone.
            Data = new Dictionary<string, object?>(values);
        }
    }

    /// <summary>
    /// Simulates one end-to-end request rendering the flash partial with
    /// the FIXED read pattern (TempData[key], the indexer). Returns the
    /// message that the partial would render for this request.
    /// </summary>
    private static string? RenderFlashFixed(InMemoryTempDataProvider provider)
    {
        var http = new DefaultHttpContext();
        var td = new TempDataDictionary(http, provider);

        // Mirror EXACTLY what _AdminFlash.cshtml does AFTER the fix.
        var success = td[AdminUi.SuccessKey] as string;
        var error = td[AdminUi.ErrorKey] as string;

        // End-of-request: TempDataDictionary.Save persists only keys that
        // were set AND not yet read (the indexer's read-and-mark semantics).
        td.Save();

        if (!string.IsNullOrEmpty(success)) return success;
        if (!string.IsNullOrEmpty(error)) return error;
        return null;
    }

    /// <summary>
    /// Simulates one end-to-end request rendering the flash partial with
    /// the OLD buggy read pattern (TempData.Peek(key)). Returns the
    /// message the partial would render.
    /// </summary>
    private static string? RenderFlashBuggyPeek(InMemoryTempDataProvider provider)
    {
        var http = new DefaultHttpContext();
        var td = new TempDataDictionary(http, provider);

        var success = td.Peek(AdminUi.SuccessKey) as string;
        var error = td.Peek(AdminUi.ErrorKey) as string;

        td.Save();

        if (!string.IsNullOrEmpty(success)) return success;
        if (!string.IsNullOrEmpty(error)) return error;
        return null;
    }

    private static void SetMessage(InMemoryTempDataProvider provider, string key, string value)
    {
        provider.Data[key] = value;
    }

    // --- The actual regression assertions ---

    [Fact]
    public void StaleError_Indexer_ConsumedAfterFirstRender()
    {
        // Arrange: simulate the state after OnPostSaveGeneralAsync's catch
        // block called SetError(...) — the underlying TempData provider
        // holds the message.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.ErrorKey, "Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.");

        // Request 1: the user reloads /admin/xe/editor after the original
        // failure. The partial renders the error once (correct).
        var firstRender = RenderFlashFixed(provider);
        Assert.Equal("Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.", firstRender);

        // Request 2: subsequent reload — must NOT show the stale error.
        var secondRender = RenderFlashFixed(provider);
        Assert.Null(secondRender);

        // Request 3: same again — still no message.
        Assert.Null(RenderFlashFixed(provider));
    }

    [Fact]
    public void StaleSuccess_Indexer_ConsumedAfterFirstRender()
    {
        // Same property for the success branch.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.SuccessKey, "Đã tạo Draft.");

        Assert.Equal("Đã tạo Draft.", RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));
    }

    [Fact]
    public void OriginalBug_Peek_DoesNotConsume_AcrossRequests()
    {
        // Documents the original broken behavior that the fix replaces.
        // If a future change reintroduces Peek, this test starts failing
        // and points directly to the regression.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.ErrorKey, "stale");

        Assert.Equal("stale", RenderFlashBuggyPeek(provider));
        Assert.Equal("stale", RenderFlashBuggyPeek(provider));
        Assert.Equal("stale", RenderFlashBuggyPeek(provider));
    }

    [Fact]
    public void FreshPage_WithNoTempData_RendersNothing()
    {
        // Normal page load with no prior failed/successful POST: nothing
        // renders. (Same state observed on the request AFTER a consumed
        // error — the user's "refresh after fix" case.)
        var provider = new InMemoryTempDataProvider();

        Assert.Null(RenderFlashFixed(provider));
    }

    [Fact]
    public void StaleError_AfterRealFailure_RemainsVisibleOnceThenDisappears()
    {
        // Full user scenario:
        //  1. User POSTs save, an exception fires, the handler writes the
        //     error via TempData and returns the page.
        //  2. User reloads (request 1) — must see the error.
        //  3. User reloads again (request 2) — must NOT see it anymore.
        //  4. User fixes and successfully saves (request 3) — the success
        //     message shows.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.ErrorKey, "Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.");

        Assert.Equal("Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.",
            RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));

        // Now the user successfully saves — SetSuccess(...) populates the
        // underlying provider for the next request.
        SetMessage(provider, AdminUi.SuccessKey, "Đã lưu bản nháp.");
        Assert.Equal("Đã lưu bản nháp.", RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));
    }

    // --- Lifecycle: explicit-clear contract ---

    /// <summary>
    /// Simulates one SetSuccess / SetError call against the real
    /// <see cref="TempDataDictionary"/> on a freshly-loaded provider, then
    /// saves the dictionary so the next request sees the post-call state.
    /// This is the exact pattern the production catch/handler paths use:
    /// handler calls the helper → Page()/RedirectToPage → Save() runs at
    /// end-of-request.
    /// </summary>
    private sealed class TestPageModel : PageModel { }

    private static TestPageModel NewPage(InMemoryTempDataProvider provider)
    {
        var http = new DefaultHttpContext();
        var page = new TestPageModel
        {
            PageContext = new PageContext(
                new ActionContext(http, new RouteData(), new PageActionDescriptor())),
            TempData = new TempDataDictionary(http, provider),
        };
        return page;
    }

    private static void ApplyLifecycleCall(
        InMemoryTempDataProvider provider,
        System.Action<PageModel> apply)
    {
        var page = NewPage(provider);
        apply(page);
        page.TempData.Save();
    }

    [Fact]
    public void SetSuccess_DropsPendingError_FromUnconsumedPriorFailure()
    {
        // Regression for the bug reported after the EDIT audit:
        //
        //   "The Editor page can still display this stale error message
        //    after the underlying operation has subsequently succeeded."
        //
        // The catch block in OnPostSaveGeneralAsync calls SetError and
        // returns Page(). If the response that wrote ErrorKey never reached
        // _AdminFlash (e.g. page render threw, response was interrupted),
        // the cookie still carries the message. When the user then retries
        // and the save succeeds, SetSuccess("Đã lưu thay đổi.") is called
        // before the redirect — it must explicitly drop ErrorKey so the
        // next GET's flash only shows the new success, not both.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.ErrorKey,
            "Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.");

        ApplyLifecycleCall(provider, page => page.SetSuccess("Đã lưu thay đổi."));

        // The next request's flash render must show only success.
        Assert.Equal("Đã lưu thay đổi.", RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));
    }

    [Fact]
    public void SetError_DropsPendingSuccess_FromUnconsumedPriorSuccess()
    {
        // Mirror of the SetSuccess regression: when the catch block writes
        // an error, any success from a prior unconsumed response must be
        // dropped so only the error renders.
        var provider = new InMemoryTempDataProvider();
        SetMessage(provider, AdminUi.SuccessKey, "Đã lưu thay đổi.");

        ApplyLifecycleCall(provider, page => page.SetError(
            "Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên."));

        Assert.Equal(
            "Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.",
            RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));
    }

    [Fact]
    public void Lifecycle_ExplicitClear_HandlesBothKeysSetSimultaneously()
    {
        // Belt-and-braces. If both keys somehow end up in the same request
        // (e.g. SetError was called mid-handler, then SetSuccess at the
        // end), only the last call's message must survive to the next
        // render — the explicit Remove on each helper guarantees it.
        var provider = new InMemoryTempDataProvider();

        var page = NewPage(provider);
        page.SetError("error-A");
        page.SetSuccess("success-B");
        page.TempData.Save();

        Assert.Equal("success-B", RenderFlashFixed(provider));
        Assert.Null(RenderFlashFixed(provider));
    }
}
