using HieuNga.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin;

public class IndexModel(
    IMotorcycleRepository motorcycles,
    IPromotionRepository promotions,
    IBlogRepository blog,
    IRepository<Domain.Entities.Booking> bookings) : PageModel
{
    public int MotorcycleCount { get; private set; }
    public int PromotionCount { get; private set; }
    public int BlogCount { get; private set; }
    public int BookingCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        MotorcycleCount = (await motorcycles.GetAllAsync(ct)).Count;
        PromotionCount = (await promotions.GetAllAsync(ct)).Count;
        BlogCount = (await blog.GetPublishedCountAsync(ct: ct));
        BookingCount = (await bookings.GetAllAsync(ct)).Count;
    }
}
