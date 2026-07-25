using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>Legacy edit URL — redirects to unified Editor (Sprint 2.1).</summary>
public class SuaModel : PageModel
{
    public IActionResult OnGet(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "general" });

    public IActionResult OnPost(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "general" });
}
