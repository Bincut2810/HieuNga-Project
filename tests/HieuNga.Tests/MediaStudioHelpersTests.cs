using HieuNga.Application.Media;

namespace HieuNga.Tests;

public class MediaStudioHelpersTests
{
    [Theory]
    [InlineData("001.jpg", 1)]
    [InlineData("frame_12.png", 12)]
    [InlineData("spin036.webp", 36)]
    [InlineData("photo.jpg", null)]
    public void TryParseFrameNumber_reads_trailing_digits(string name, int? expected)
    {
        Assert.Equal(expected, MediaFrameNaming.TryParseFrameNumber(name));
    }
}
