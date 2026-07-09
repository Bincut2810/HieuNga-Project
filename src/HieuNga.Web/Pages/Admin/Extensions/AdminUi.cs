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
