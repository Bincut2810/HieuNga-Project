using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>Legacy create URL — redirects to unified Editor create mode.</summary>
public class ThemModel : PageModel
{
    public IActionResult OnGet() =>
        RedirectToPagePermanent("./Editor", new { tab = "general" });

    public IActionResult OnPost() =>
        RedirectToPagePermanent("./Editor", new { tab = "general" });
}
