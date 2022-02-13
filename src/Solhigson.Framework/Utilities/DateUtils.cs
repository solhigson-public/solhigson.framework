using System;
using System.Globalization;

namespace Solhigson.Framework.Utilities
{
    public static class DateUtils
    {
        /// <summary>
        /// default culture info
        /// </summary>
        private static CultureInfo _ci;

        /// <summary>
        /// Gets the default date time picker format.
        /// </summary>
        public static string DefaultDateFormat => "dd/MMM/yyyy HH:mm";

        public static string SearchDateFormat => "dd/MMM/yyyy HH:mm:ss";

        public static string DefaultShortDateFormat => "dd/MMM/yyyy";
        //old one
        //return "dd/MM/yyyy HH:mm";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //public static DateTime Epoch
        //{
        //    get { return _epoch; }
        //}

        public static double CurrentUnixTimestamp => ToUnixTimestamp(DateTime.UtcNow);

        public static double ToUnixTimestamp(DateTime datetime)
        {
            var timeDiff = datetime.ToUniversalTime() - Epoch;
            return Math.Floor(timeDiff.TotalSeconds);
        }

        public static DateTime FromUnixTimestamp(double unixTimestamp)
        {
            return Epoch.AddSeconds(unixTimestamp);
        }

        /// <summary>
        /// Gets the default culture info.
        /// </summary>
        public static CultureInfo DefaultCultureInfo
        {
            get
            {
                if (_ci == null)
                {
                    _ci = new CultureInfo("") { DateTimeFormat = { LongDatePattern = DefaultDateFormat, ShortDatePattern = DefaultShortDateFormat } };
                }
                return _ci;
            }
        }

        public static DateTime ParseStringExact(string dateTimeString, string format, DateTime? valueAsDefault = null)
        {
            var ci = new CultureInfo("") { DateTimeFormat = { LongDatePattern = format } };
            var res = ParseStringExact(dateTimeString, ci, out var result, valueAsDefault);
            return result;
        }

        public static bool TryParseStringExact(string dateTimeString, out DateTime result, DateTime? valueAsDefault = null)
        {
            return ParseStringExact(dateTimeString, DefaultCultureInfo, out result, valueAsDefault, false);
        }

        public static DateTime ParseStringExact(string dateTimeString, DateTime? valueAsDefault = null)
        {
            var res = ParseStringExact(dateTimeString, DefaultCultureInfo, out var result, valueAsDefault);
            return result;
        }

        public static bool ParseStringExact(string dateTimeString, CultureInfo cultureInfo, out DateTime result, DateTime? valueAsDefault = null,
            bool throwException = true)
        {
            result = DateTime.UtcNow;
            try
            {
                result = DateTime.Parse(dateTimeString, cultureInfo);
                return true;
            }
            catch (Exception)
            {
                if (valueAsDefault.HasValue)
                {
                    result = valueAsDefault.Value;
                }
                else if (throwException)
                {
                    throw;
                }
                return false;
            }
        }

        public static string FormatToDDMMMYY(DateTime dte)
        {
            return dte.ToString("dd-MMM-yy", (IFormatProvider)DateTimeFormatInfo.InvariantInfo) + " " + dte.ToString("T", (IFormatProvider)DateTimeFormatInfo.InvariantInfo);
        }

        //Returns that last day of any specified Date
        public static int LastDayOfMonth(DateTime currentDate)
        {
            return System.DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
        }

        public static string MonthInShortWords(int month)
        {
            var result = "Invalid";
            switch (month)
            {
                case 1:
                    result = "Jan";
                    break;
                case 2:
                    result = "Feb";
                    break;
                case 3:
                    result = "Mar";
                    break;
                case 4:
                    result = "Apr";
                    break;
                case 5:
                    result = "May";
                    break;
                case 6:
                    result = "June";
                    break;
                case 7:
                    result = "July";
                    break;
                case 8:
                    result = "Aug";
                    break;
                case 9:
                    result = "Sep";
                    break;
                case 10:
                    result = "Oct";
                    break;
                case 11:
                    result = "Nov";
                    break;
                case 12:
                    result = "Dec";
                    break;
            }
            return result;
        }
        public static string DateToWords(DateTime date, bool includeDay = false)
        {
            var result = MonthInShortWords(date.Month);

            if (!includeDay)
            {
                return result + " " + date.Year;
            }
            var day = date.Day.ToString(CultureInfo.InvariantCulture);
            if (date.Day > 10 && date.Day < 20)
            {
                day += "th";
            }
            else
            {
                var modulus = date.Day % 10;
                switch (modulus)
                {
                    case 1:
                        day += "st";
                        break;
                    case 2:
                        day += "nd";
                        break;
                    case 3:
                        day += "rd";
                        break;
                    default:
                        day += "th";
                        break;
                }
            }
            result = day + " " + result;
            return result + " " + date.Year;
        }

        public static bool IsLastDayOfMonth(DateTime dateTime)
        {
            var daysInMonth = DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
            return dateTime.Day == daysInMonth;
        }

        public static (DateTime FromDate, DateTime ToDate) TodayDateRange(bool utc = true)
        {
            var anchor = DateTime.Now;
            var timeZoneDiff = anchor.ToUniversalTime() - anchor;
            var fromDate = anchor.Date;
            if (utc)
            {
                fromDate = fromDate.Add(timeZoneDiff);
            }
            var toDate = fromDate.AddDays(1).AddMilliseconds(-1);
            return (fromDate, toDate);
        }
        
        public static (DateTime FromDate, DateTime ToDate) DayDateRange(this DateTime dateTime)
        {
            var fromDate = dateTime.Date;
            var toDate = fromDate.AddDays(1).AddMilliseconds(-1);
            return (fromDate, toDate);
        }
        
        public static (DateTime FromDate, DateTime ToDate) ThisMonthDateRange(bool utc = true)
        {
            var anchor = DateTime.Now;
            var timeZoneDiff = anchor.ToUniversalTime() - anchor;
            var fromDate = new DateTime(anchor.Year, anchor.Month, 1);
            if (utc)
            {
                fromDate = fromDate.Add(timeZoneDiff);
            }

            var dateToUse = fromDate;
            if (timeZoneDiff.TotalMinutes < 0)
            {
                dateToUse = anchor;
            }

            var toDate = fromDate.AddDays(LastDayOfMonth(dateToUse)).AddMilliseconds(-1);

            return (fromDate, toDate);
        }

                /// <summary>
        /// Checks if the current date is working day or not. Assuming 
        /// </summary>
        /// <param name="currentDate">
        /// The current date.
        /// </param>
        /// <returns>
        /// true if current date is a working day otherwise false
        /// </returns>
        public static bool IsWorkingDay(this DateTime currentDate)
        {
            return currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>
        /// Gets the first day of the month
        /// </summary>
        /// <param name="current">
        /// The current date
        /// </param>
        /// <returns>
        /// date of the first day of the month
        /// </returns>
        public static DateTime First(this DateTime current)
        {
            return current.AddDays((double)(1 - current.Day));
        }

        /// <summary>
        /// Gets the date of first certain day of the week of the month
        /// </summary>
        /// <param name="current">
        /// The current date
        /// </param>
        /// <param name="dayOfWeek">day of the week</param>
        /// <returns>
        /// date of the first day of the month
        /// </returns>
        public static DateTime First(this DateTime current, DayOfWeek dayOfWeek)
        {
            DateTime dateTime = current.First();
            if (dateTime.DayOfWeek != dayOfWeek)
            {
                dateTime = dateTime.Next(dayOfWeek);
            }

            return dateTime;
        }

        /// <summary>
        /// Gets the last day of the month
        /// </summary>
        /// <param name="current">
        /// The current date
        /// </param>
        /// <returns>
        /// date of the last day of the month
        /// </returns>
        public static DateTime Last(this DateTime current)
        {
            int num = DateTime.DaysInMonth(current.Year, current.Month);
            return current.First().AddDays((double)(num - 1));
        }

        /// <summary>
        /// Gets the date of last certain day of the week of the month
        /// </summary>
        /// <param name="current">
        /// The current date
        /// </param>
        /// <param name="dayOfWeek">day of the week</param>
        /// <returns>
        /// date of the last day of the month
        /// </returns>
        public static DateTime Last(this DateTime current, DayOfWeek dayOfWeek)
        {
            DateTime result = current.Last();
            result = result.AddDays((double)(Math.Abs((int)(dayOfWeek - result.DayOfWeek)) * -1));
            return result;
        }

        /// <summary>
        /// Gets the date of next certain day of the week of the month
        /// </summary>
        /// <param name="current">
        /// The current date
        /// </param>
        /// <param name="dayOfWeek">day of the week</param>
        /// <returns>
        /// date of the next day of the month
        /// </returns>
        public static DateTime Next(this DateTime current, DayOfWeek dayOfWeek)
        {
            int num = (int)(dayOfWeek - current.DayOfWeek);
            if (num <= 0)
            {
                num += 7;
            }

            return current.AddDays((double)num);
        }

        public static bool IsFirstDayOfMonth(this DateTime dateTime)
        {
            return dateTime.Day == 1;
        }

        public static int NumberOfDaysInMonth(this DateTime dateTime)
        {
            return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
        }

        
    }
}