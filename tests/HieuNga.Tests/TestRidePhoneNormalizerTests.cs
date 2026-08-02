using HieuNga.Application.TestRide;

namespace HieuNga.Tests;

public class TestRidePhoneNormalizerTests
{
    [Theory]
    [InlineData("0905123456", "0905123456")]
    [InlineData("+84905123456", "0905123456")]
    [InlineData("84905123456", "0905123456")]
    [InlineData(" 0905 123 456 ", "0905123456")]
    public void Normalize_Unifies_Vn_Mobile_Forms(string input, string expected)
    {
        Assert.Equal(expected, TestRidePhoneNormalizer.Normalize(input));
    }

    [Fact]
    public void LookupVariants_Include_Common_Forms()
    {
        var variants = TestRidePhoneNormalizer.LookupVariants("+84905123456");
        Assert.Contains("0905123456", variants);
        Assert.Contains("84905123456", variants);
        Assert.Contains("+84905123456", variants);
    }

    [Fact]
    public void VietnamTime_Today_Is_Date_Only()
    {
        var today = TestRideVietnamTime.Today;
        Assert.Equal(TimeSpan.Zero, today.TimeOfDay);
        Assert.True((TestRideVietnamTime.UtcNow - DateTime.UtcNow).Duration() < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ConvertLocalAppointmentDateToUtc_Is_Always_Utc_Kind()
    {
        var unspecified = DateTime.Parse("2026-08-02");
        Assert.Equal(DateTimeKind.Unspecified, unspecified.Kind);

        var utc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(unspecified);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        // VN midnight 2026-08-02 = 2026-08-01 17:00 UTC (UTC+7)
        Assert.Equal(new DateTime(2026, 8, 1, 17, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void ConvertLocalAppointmentDateEndExclusiveToUtc_Is_Next_Day_Start()
    {
        var start = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(DateTime.Parse("2026-08-02"));
        var end = TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(DateTime.Parse("2026-08-02"));
        Assert.Equal(DateTimeKind.Utc, end.Kind);
        Assert.Equal(start.AddDays(1), end);
    }

    [Theory]
    [InlineData("2026-08-02")]
    [InlineData("2026-12-31")]
    [InlineData("2027-01-01")]
    public void ConvertLocalAppointmentDateToUtc_Ignores_Input_Kind(string isoDate)
    {
        var calendar = DateTime.Parse(isoDate);
        var asUtcKind = DateTime.SpecifyKind(calendar, DateTimeKind.Utc);
        var asLocalKind = DateTime.SpecifyKind(calendar, DateTimeKind.Local);

        var a = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(calendar);
        var b = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(asUtcKind);
        var c = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(asLocalKind);

        Assert.Equal(a, b);
        Assert.Equal(a, c);
        Assert.Equal(DateTimeKind.Utc, a.Kind);
    }

    [Fact]
    public void ToVietnamDate_Maps_Utc_Midnight_Offset()
    {
        var stored = new DateTime(2026, 8, 1, 17, 0, 0, DateTimeKind.Utc);
        var vn = TestRideVietnamTime.ToVietnamDate(stored);
        Assert.Equal(new DateTime(2026, 8, 2), vn.Date);
    }
}
