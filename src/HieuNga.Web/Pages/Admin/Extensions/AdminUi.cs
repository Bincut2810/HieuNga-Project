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
    string SaveText = "Save Draft",
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
