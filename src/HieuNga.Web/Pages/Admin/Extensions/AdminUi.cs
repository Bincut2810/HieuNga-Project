using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Extensions;

public static class AdminUi
{
    public const string SuccessKey = "AdminSuccess";
    public const string ErrorKey = "AdminError";

    public static void SetSuccess(this PageModel page, string message) =>
        page.TempData[SuccessKey] = message;

    public static void SetError(this PageModel page, string message) =>
        page.TempData[ErrorKey] = message;

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
/// Model for the Admin/Shared/_ImagePickerField partial.
/// Drives a single-image dropzone widget that writes a URL back into
/// the bound hidden input. Used for Promotion, BlogPost and Bank logos.
///
/// <param name="AspFor">
///   The form field name the picker should write to. This must match
///   the existing `name` attribute that the surrounding form would have
///   produced (e.g. "ImageUrl", "ThumbnailUrl", "Input.LogoUrl").
/// </param>
/// <param name="Kind">
///   Storage folder key passed to /admin/api/image-upload.
///   Recognized values: "promotions", "blog", "banks".
/// </param>
/// <param name="Label">
///   Vietnamese field label shown above the widget (e.g. "Ảnh đại diện").
/// </param>
/// <param name="Hint">
///   Short call-to-action shown inside the empty dropzone
///   (e.g. "Kéo ảnh vào đây hoặc bấm để chọn ảnh"). Falls back to a JS default.
/// </param>
/// <param name="HelpText">
///   Supporting text shown below the widget (e.g. "JPG, PNG hoặc WebP • Tối đa 5 MB").
/// </param>
/// <param name="CurrentValue">
///   Existing URL to display as the initial preview (taken from the bound model property).
/// </param>
/// </summary>
public record ImagePickerField(
    string AspFor,
    string Kind,
    string Label,
    string? Hint = null,
    string? HelpText = null,
    string? CurrentValue = null);
