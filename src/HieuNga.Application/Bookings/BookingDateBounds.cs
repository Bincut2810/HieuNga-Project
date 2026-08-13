using HieuNga.Application.TestRide;

namespace HieuNga.Application.Bookings;

/// <summary>Vietnam calendar day → UTC timestamptz bounds for SQL predicates.</summary>
public readonly record struct BookingDateBounds(
    DateTime TodayUtc,
    DateTime TomorrowUtc,
    DateTime DayAfterTomorrowUtc,
    DateTime WeekEndUtc)
{
    public static BookingDateBounds ForVietnamToday()
    {
        var todayVn = TestRideVietnamTime.Today;
        return new BookingDateBounds(
            TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn),
            TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(todayVn),
            TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn.AddDays(2)),
            TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn.AddDays(7)));
    }
}
