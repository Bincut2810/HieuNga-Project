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
}
