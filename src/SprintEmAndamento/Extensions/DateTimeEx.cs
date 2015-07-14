using System;

namespace CoreSprint.Extensions
{
    public static class DateTimeEx
    {
        public static string ToHumanReadable(this DateTime dateTime)
        {
            var dateTimeStr = string.Format("{0} {1}", dateTime.ToShortDateString(), dateTime.ToShortTimeString());
            return dateTimeStr;
        }
    }
}
