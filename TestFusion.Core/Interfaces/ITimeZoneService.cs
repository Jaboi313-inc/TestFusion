namespace TestFusion.Core.Interfaces;

public interface ITimeZoneService
{
    string SourceTimeZoneId { get; }

    string UserTimeZoneId { get; }

    DateTimeOffset ConvertSourceToUtc(
        DateTime sourceDateTime);

    DateTimeOffset ParseSourceToUtc(
        string sourceDateTime);

    DateTimeOffset ConvertUtcToUser(
        DateTimeOffset utcDateTime);

    string FormatForUser(
        DateTimeOffset utcDateTime,
        string format = "dd-MM-yyyy HH:mm");
}