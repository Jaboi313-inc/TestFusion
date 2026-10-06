using System.Globalization;
using TestFusion.Core.Interfaces;

namespace TestFusion.Services.Services;

public class TimeZoneService : ITimeZoneService
{
    public string SourceTimeZoneId { get; } = "Europe/Amsterdam";

    public string UserTimeZoneId { get; } = "Europe/Amsterdam";


    public DateTimeOffset ConvertSourceToUtc(DateTime sourceDateTime)
    {
        var sourceTimeZone = TimeZoneInfo.FindSystemTimeZoneById(SourceTimeZoneId);

        var unspecifiedDateTime = DateTime.SpecifyKind(sourceDateTime, DateTimeKind.Unspecified);

        if (sourceTimeZone.IsInvalidTime(unspecifiedDateTime))
        {
            throw new InvalidOperationException($"The datetime '{sourceDateTime}' does not exist in timezone '{SourceTimeZoneId}'.");
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedDateTime, sourceTimeZone);

        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }


    public DateTimeOffset ParseSourceToUtc(string sourceDateTime)
    {
        if (string.IsNullOrWhiteSpace(sourceDateTime))
        {
            throw new ArgumentException("Source datetime cannot be empty.", nameof(sourceDateTime));
        }

        if (!DateTime.TryParseExact(
            sourceDateTime,
            "dd/MM/yyyy HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDateTime))
        {
            throw new FormatException($"Invalid source datetime '{sourceDateTime}'. Expected format: dd/MM/yyyy HH:mm:ss.");
        }

        return ConvertSourceToUtc(parsedDateTime);
    }


    public DateTimeOffset ConvertUtcToUser(DateTimeOffset utcDateTime)
    {
        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserTimeZoneId);

        return TimeZoneInfo.ConvertTime(utcDateTime.ToUniversalTime(), userTimeZone);
    }


    public string FormatForUser(DateTimeOffset utcDateTime, string format = "dd-MM-yyyy HH:mm")
    {
        return ConvertUtcToUser(utcDateTime).ToString(format, CultureInfo.InvariantCulture);
    }
}