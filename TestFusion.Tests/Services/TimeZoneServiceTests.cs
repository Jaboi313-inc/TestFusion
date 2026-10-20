using TestFusion.Services.Services;

namespace TestFusion.Tests.Services;

public class TimeZoneServiceTests
{
    private readonly TimeZoneService _service = new();

    [Fact]
    public void SourceAndUserTimeZone_AreEuropeAmsterdam()
    {
        Assert.Equal("Europe/Amsterdam", _service.SourceTimeZoneId);
        Assert.Equal("Europe/Amsterdam", _service.UserTimeZoneId);
    }

    [Fact]
    public void ConvertSourceToUtc_InWinter_UsesUtcPlusOne()
    {
        var result = _service.ConvertSourceToUtc(new DateTime(2026, 1, 15, 12, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void ConvertSourceToUtc_InSummer_UsesUtcPlusTwo()
    {
        var result = _service.ConvertSourceToUtc(new DateTime(2026, 7, 15, 12, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void ConvertSourceToUtc_InvalidDstTime_Throws()
    {
        var invalidTime = new DateTime(2026, 3, 29, 2, 30, 0);

        Assert.Throws<InvalidOperationException>(() => _service.ConvertSourceToUtc(invalidTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseSourceToUtc_EmptyValue_ThrowsArgumentException(string? input)
    {
        Assert.Throws<ArgumentException>(() => _service.ParseSourceToUtc(input!));
    }

    [Fact]
    public void ParseSourceToUtc_InvalidFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => _service.ParseSourceToUtc("2026-10-18 15:00:00"));
    }

    [Fact]
    public void ParseSourceToUtc_ValidValue_ReturnsUtc()
    {
        var result = _service.ParseSourceToUtc("18/10/2026 15:00:00");

        Assert.Equal(new DateTimeOffset(2026, 10, 18, 13, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void ConvertUtcToUser_ConvertsToAmsterdamTime()
    {
        var result = _service.ConvertUtcToUser(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(12, result.Hour);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
    }

    [Fact]
    public void ConvertUserToUtc_ConvertsAmsterdamTimeToUtc()
    {
        var result = _service.ConvertUserToUtc(new DateTime(2026, 7, 15, 12, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void FormatForUser_UsesRequestedFormat()
    {
        var result = _service.FormatForUser(
            new DateTimeOffset(2026, 7, 15, 10, 5, 0, TimeSpan.Zero),
            "dd/MM/yyyy HH:mm");

        Assert.Equal("15/07/2026 12:05", result);
    }
}
