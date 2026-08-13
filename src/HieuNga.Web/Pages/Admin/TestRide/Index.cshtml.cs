using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HieuNga.Web.Pages.Admin.Bookings;

namespace HieuNga.Web.Pages.Admin.TestRide;

/// <summary>Compatibility redirect to unified Booking Center — preserves every query value.</summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet() => BookingCenterRedirect.ToCenter(Request, "testride");
}
