using HieuNga.Domain.Enums;

namespace HieuNga.Application.Bookings;

/// <summary>
/// SQL-first admin board query. Filtering happens in the repository predicate — not after materialization.
/// Paging fields are applied when both <see cref="Page"/> and <see cref="PageSize"/> are set.
/// </summary>
public sealed class BookingQuery
{
    public BookingType? BookingType { get; init; }
    public string? DateRange { get; init; }
    public BookingStatus? Status { get; init; }
    public string? Search { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }

    public string NormalizedRange => NormalizeRange(DateRange);

    public string? NormalizedSearch
    {
        get
        {
            var q = Search?.Trim();
            return string.IsNullOrEmpty(q) ? null : q;
        }
    }

    public bool HasPaging =>
        Page is > 0 && PageSize is > 0;

    public int Skip => HasPaging ? (Page!.Value - 1) * PageSize!.Value : 0;
    public int? Take => HasPaging ? PageSize : null;

    /// <summary>
    /// Maps Booking Center / legacy admin URL params into a typed query.
    /// <paramref name="range"/> values: today|tomorrow|week|late|completed|cancelled|all.
    /// </summary>
    public static BookingQuery FromAdmin(
        string? range,
        string? search,
        BookingType? bookingType = null,
        int? page = null,
        int? pageSize = null)
    {
        var r = NormalizeRange(range);
        return r switch
        {
            "completed" => new BookingQuery
            {
                BookingType = bookingType,
                DateRange = "all",
                Status = BookingStatus.Completed,
                Search = search,
                Page = page,
                PageSize = pageSize
            },
            "cancelled" => new BookingQuery
            {
                BookingType = bookingType,
                DateRange = "all",
                Status = BookingStatus.Cancelled,
                Search = search,
                Page = page,
                PageSize = pageSize
            },
            _ => new BookingQuery
            {
                BookingType = bookingType,
                DateRange = r,
                Status = null,
                Search = search,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public static string NormalizeRange(string? range) =>
        (range ?? "today").Trim().ToLowerInvariant() switch
        {
            "tomorrow" => "tomorrow",
            "week" => "week",
            "late" => "late",
            "completed" => "completed",
            "cancelled" => "cancelled",
            "all" => "all",
            _ => "today"
        };
}
