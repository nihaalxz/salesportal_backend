using System;

namespace TexasPortal.Domain.Common;

/// <summary>
/// Centralized Bahrain Timezone (UTC+3, Arab Standard Time) utility.
/// Bahrain does not observe Daylight Saving Time (DST) and operates on fixed UTC+3.
/// </summary>
public static class BahrainTime
{
    private static readonly Lazy<TimeZoneInfo> _timeZone = new(() =>
    {
        try
        {
            // Windows ID
            return TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
        }
        catch
        {
            try
            {
                // IANA / Linux ID
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bahrain");
            }
            catch
            {
                // Fallback fixed UTC+3
                return TimeZoneInfo.CreateCustomTimeZone("Bahrain_Standard_Time", TimeSpan.FromHours(3), "Bahrain Time", "Bahrain Time");
            }
        }
    });

    /// <summary>
    /// The Bahrain TimeZoneInfo instance.
    /// </summary>
    public static TimeZoneInfo TimeZone => _timeZone.Value;

    /// <summary>
    /// Gets current date and time in Bahrain (UTC+3).
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    /// <summary>
    /// Gets current calendar Date in Bahrain (at 00:00:00).
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Converts a UTC DateTime to Bahrain local DateTime.
    /// </summary>
    public static DateTime ToBahrainTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    /// <summary>
    /// Converts a Bahrain local DateTime to UTC DateTime.
    /// </summary>
    public static DateTime ToUtc(DateTime bahrainDateTime)
    {
        var local = DateTime.SpecifyKind(bahrainDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, TimeZone);
    }

    /// <summary>
    /// Converts Bahrain local start and end dates (e.g. from user filters) to exact UTC boundaries.
    /// </summary>
    public static (DateTime? startUtc, DateTime? endUtc) GetUtcRange(DateTime? startBahrainDate, DateTime? endBahrainDate)
    {
        DateTime? startUtc = null;
        if (startBahrainDate.HasValue)
        {
            startUtc = ToUtc(startBahrainDate.Value.Date);
        }

        DateTime? endUtc = null;
        if (endBahrainDate.HasValue)
        {
            endUtc = ToUtc(endBahrainDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        return (startUtc, endUtc);
    }
}
