using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Extensions;

public static class AdminUi
{
    public const string SuccessKey = "AdminSuccess";
    public const string ErrorKey = "AdminError";

    /// <summary>
    /// Write a success flash and drop any pending error so a previous failed
    /// save's banner cannot re-render alongside the new success message.
    /// Without the explicit Remove, the cookie would still carry the stale
    /// ErrorKey if the previous response did not reach the _AdminFlash
    /// partial (e.g. page render failed before the layout ran).
    /// </summary>
    public static void SetSuccess(this PageModel page, string message)
    {
        page.TempData[SuccessKey] = message;
        page.TempData.Remove(ErrorKey);
    }

    /// <summary>
    /// Write an error flash and drop any pending success for the same
    /// lifecycle reason as <see cref="SetSuccess"/>: only the most recent
    /// message should be visible on the next render.
    /// </summary>
    public static void SetError(this PageModel page, string message)
    {
        page.TempData[ErrorKey] = message;
        page.TempData.Remove(SuccessKey);
    }

    public static string? PeekSuccess(this PageModel page) =>
        page.TempData.Peek(SuccessKey) as string;

    public static string? PeekError(this PageModel page) =>
        page.TempData.Peek(ErrorKey) as string;
}

public record AdminPageHeaderModel(
    string Title,
    string? Subtitle = null,
    string? PrimaryActionUrl = null,
    string? PrimaryActionText = null);

public record AdminBreadcrumbItem(string Text, string? Url = null);

public record AdminEmptyStateModel(
    string Title,
    string? Text = null,
    string? ActionUrl = null,
    string? ActionText = null);

/// <summary>Simple list row for Content modules (promotions, news, branches).</summary>
public record AdminListItemModel(
    string Title,
    string? Eyebrow = null,
    string? Meta = null,
    string? EditUrl = null,
    string? ViewUrl = null,
    string EditText = "Sửa",
    string ViewText = "Xem →");

public record EditorSaveBarModel(
    string? PreviewSlug,
    string SaveText = "Lưu bản nháp",
    string? PublishTabUrl = null,
    bool IsPublishTab = false)
{
    public string? PreviewUrl => string.IsNullOrEmpty(PreviewSlug) ? null : $"/xe/{PreviewSlug}";
}

public record ContentCardItemModel(Guid Id, string Title, string? Description, string ImageUrl, int SortOrder);

public record ContentCardBuilderModel(
    Guid MotorcycleId,
    string Kind,
    string Title,
    string Subtitle,
    string AddHandler,
    string UpdateHandler,
    string DeleteHandler,
    string DuplicateHandler,
    string ReorderHandler,
    bool SupportsUpload,
    IReadOnlyList<ContentCardItemModel> Items);

/// <summary>
/// Model for the Admin/Shared/_ImageUploader partial.
/// Drives the shared dropzone widget for both single-image forms (Promotion,
/// BlogPost, Bank) and domain-endpoint integrations (motorcycle media, banner,
/// service gallery).
/// </summary>
public record ImageUploaderField(
    string Kind,
    string? Label = null,
    string? Hint = null,
    string? HelpText = null,

    /// <summary>form-input mode only — the name of the hidden input the uploader writes to.</summary>
    string? AspFor = null,

    /// <summary>form-input mode only — the current URL used for the initial preview.</summary>
    string? CurrentValue = null,

    /// <summary>"form-input" (default) or "domain" (POST URL to <see cref="Endpoint"/>).</summary>
    string? Mode = null,

    /// <summary>domain mode only — endpoint that receives { url } in a JSON body.</summary>
    string? Endpoint = null,

    /// <summary>Optional context id (e.g. motorcycle id) sent as the canonical <c>contextId</c> field.</summary>
    string? ContextId = null,

    /// <summary>If true, the picker accepts multiple files (banner / service gallery).</summary>
    bool Multiple = false);
