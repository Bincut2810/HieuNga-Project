using HieuNga.Domain;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class MediaStudioHelpersTests
{
    [Theory]
    [InlineData("front.jpg", MotorcycleViewAngle.Front)]
    [InlineData("front-left.png", MotorcycleViewAngle.FrontLeft)]
    [InlineData("left.webp", MotorcycleViewAngle.Left)]
    [InlineData("rear.jpg", MotorcycleViewAngle.Rear)]
    [InlineData("right.jpg", MotorcycleViewAngle.Right)]
    [InlineData("front-right.jpg", MotorcycleViewAngle.FrontRight)]
    [InlineData("truoc.jpg", MotorcycleViewAngle.Front)]
    [InlineData("sau.png", MotorcycleViewAngle.Rear)]
    [InlineData("back.jpg", MotorcycleViewAngle.Rear)]
    [InlineData("fl.jpg", MotorcycleViewAngle.FrontLeft)]
    [InlineData("0.jpg", MotorcycleViewAngle.Front)]
    [InlineData("5.webp", MotorcycleViewAngle.FrontRight)]
    public void TryParseKey_reads_angle_keys(string name, MotorcycleViewAngle expected)
    {
        Assert.True(MotorcycleViewAngleCatalog.TryParseKey(name, out var angle));
        Assert.Equal(expected, angle);
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("gallery-01.png")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseKey_rejects_unknown(string? name)
    {
        Assert.False(MotorcycleViewAngleCatalog.TryParseKey(name, out _));
    }
}
