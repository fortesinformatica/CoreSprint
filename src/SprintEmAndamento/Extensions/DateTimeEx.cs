using System;
using System.Linq;

namespace CoreSprint.Extensions
{
    public static class DateTimeEx
    {
        public static string ToHumanReadable(this DateTime dateTime)
        {
            var dateTimeStr = string.Format("{0} {1}", dateTime.ToShortDateString(), dateTime.ToShortTimeString());
            return dateTimeStr;
        }

        public static DateTime ConvertUtcToFortalezaTimeZone(this DateTime dateTime)
        {
            var timeZone = TimeZoneInfo.GetSystemTimeZones().First(tz => tz.DisplayName.ToLower().Contains("fortaleza"));
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZone);
        }
    }
}
