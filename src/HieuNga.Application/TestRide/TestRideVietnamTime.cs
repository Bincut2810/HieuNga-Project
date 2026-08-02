namespace HieuNga.Application.TestRide;

/// <summary>
/// Central VN (UTC+7) clock for TestRide — appointment dates, "today" filters, duplicate windows.
/// </summary>
public static class TestRideVietnamTime
{
    private static readonly TimeZoneInfo Vietnam = ResolveVietnamTimeZone();

    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Vietnam);

    public static DateTime Today => Now.Date;

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
