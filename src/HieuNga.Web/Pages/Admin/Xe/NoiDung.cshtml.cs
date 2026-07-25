using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Xe;

/// <summary>Legacy content URL — redirects to Editor Media tab.</summary>
public class NoiDungModel : PageModel
{
    public IActionResult OnGet(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "media" });

    public IActionResult OnPost(Guid id) =>
        RedirectToPagePermanent("./Editor", new { id, tab = "media" });
}
