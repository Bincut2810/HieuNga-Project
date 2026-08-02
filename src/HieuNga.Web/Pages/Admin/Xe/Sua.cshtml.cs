using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>Previous edit URL — redirects to unified Editor.</summary>
public class SuaModel : PageModel
{
    public IActionResult OnGet(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "general" });

    public IActionResult OnPost(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "general" });
}
