using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>Previous price URL — redirects to Editor Finance tab.</summary>
public class GiaModel : PageModel
{
    public IActionResult OnGet(Guid id, Guid? edit) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "finance", edit });

    public IActionResult OnPost(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "finance" });
}
