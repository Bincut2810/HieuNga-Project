using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HieuNga.Web.Pages.Admin.Bookings;

/// <summary>
/// Compatibility redirects preserve every inbound query value and only set <c>type</c>.
/// No parameter renaming / translation.
/// </summary>
public static class BookingCenterRedirect
{
    public static IActionResult ToCenter(HttpRequest request, string type)
    {
        var values = new RouteValueDictionary();
        foreach (var kv in request.Query)
            values[kv.Key] = kv.Value.ToString();

        values["type"] = type;
        return new RedirectToPageResult("/Admin/Bookings/Index", values);
    }
}
