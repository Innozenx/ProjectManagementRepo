using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;

namespace ProjectManagementSystem.Extensions
{
  
        public static class DateTimeExtensions
        {
            public static string ToWorkWeekFormat(this DateTime date)
            {
                var calendar = CultureInfo.InvariantCulture.Calendar;
                var weekOfYear = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                return $"WW {date:yy}{weekOfYear:00}";
            }
        }

  
}