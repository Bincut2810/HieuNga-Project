namespace HieuNga.Application.TestRide;

/// <summary>
/// Central VN (UTC+7) clock for TestRide — appointment dates, "today" filters, duplicate windows.
/// All values sent to PostgreSQL <c>timestamptz</c> must go through
/// <see cref="ConvertLocalAppointmentDateToUtc"/> (Kind = <see cref="DateTimeKind.Utc"/>).
/// </summary>
public static class TestRideVietnamTime
{
    private static readonly TimeZoneInfo Vietnam = ResolveVietnamTimeZone();

    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Current wall clock in Vietnam. Kind is Unspecified — UI / validation only; never send to Npgsql.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Vietnam);

    /// <summary>Vietnam calendar date (midnight local). Kind Unspecified — UI / validation only; never send to Npgsql.</summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Maps a Vietnam business calendar date to the UTC instant of that day's local midnight
    /// (00:00 Asia/Ho_Chi_Minh). Always returns <see cref="DateTimeKind.Utc"/>.
    /// Sole conversion for PreferredDate persistence and day-range EF queries.
    /// </summary>
    public static DateTime ConvertLocalAppointmentDateToUtc(DateTime localCalendarDate)
    {
        var vnMidnight = new DateTime(
            localCalendarDate.Year,
            localCalendarDate.Month,
            localCalendarDate.Day,
            0, 0, 0,
            DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(vnMidnight, Vietnam);
    }

    /// <summary>
    /// Exclusive end of a Vietnam business day in UTC (= start of the next calendar day).
    /// Use with <c>PreferredDate &gt;= start &amp;&amp; PreferredDate &lt; end</c>.
    /// </summary>
    public static DateTime ConvertLocalAppointmentDateEndExclusiveToUtc(DateTime localCalendarDate) =>
        ConvertLocalAppointmentDateToUtc(
            new DateTime(
                localCalendarDate.Year,
                localCalendarDate.Month,
                localCalendarDate.Day,
                0, 0, 0,
                DateTimeKind.Unspecified).AddDays(1));

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Ho_Chi_Minh",
            TimeSpan.FromHours(7),
            "Indochina Time",
            "Indochina Time");
    }
}
