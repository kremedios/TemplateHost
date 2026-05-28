using System;

namespace Host.Models.Datetime
{
    public static class TimeMgr
    {
        public static DateTime NowOnCentralTime()
        {
            TimeZoneInfo centralTimeZone;

            try
            {
                // Linux / macOS
                centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            }
            catch (TimeZoneNotFoundException)
            {
                // Windows fallback
                centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralTimeZone);
        }




      public static string NowOnCentralTime_mm_dd()
{
    TimeZoneInfo centralTimeZone;

    try
    {
        // Linux / macOS
        centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
    }
    catch (TimeZoneNotFoundException)
    {
        // Windows fallback
        centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
    }

    var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralTimeZone);

    return centralNow.ToString("MMdd_");
}





    }
}
