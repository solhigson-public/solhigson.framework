using System.Globalization;

namespace Solhigson.Utilities;

public static class DateUtils
{
    public static double CurrentUnixTimestamp => ToUnixTimestamp(DateTime.UtcNow);

    public static double ToUnixTimestamp(this DateTime datetime, bool isUtc = true)
    {
        var dateToUse = isUtc ? datetime : datetime.ToUniversalTime();
        var timeDiff = dateToUse - DateTime.UnixEpoch;
        return Math.Floor(timeDiff.TotalSeconds);
    }
    
    public static DateTime FromUnixTimestamp(this double unixTimestamp)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTimestamp);
    }

    public static DateTime ParseStringExact(string dateTimeString, string format, DateTime? valueAsDefault = null)
    {
        var ci = new CultureInfo("") { DateTimeFormat = { LongDatePattern = format } };
        _ = ParseStringExact(dateTimeString, ci, out var result, valueAsDefault);
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

    //Returns that last day of any specified Date
    public static int DaysInMonth(DateTime currentDate)
    {
        return System.DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
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
    public static DateTime LastDateOfMonth(this DateTime current)
    {
        int num = DateTime.DaysInMonth(current.Year, current.Month);
        return current.FirstDayOfMonth().AddDays((double)(num - 1));
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
        if (date.Day is > 10 and < 20)
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

        var toDate = fromDate.AddDays(DaysInMonth(dateToUse)).AddMilliseconds(-1);

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
    public static DateTime FirstDayOfMonth(this DateTime current)
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
    public static DateTime FirstWeekDayOfMonth(this DateTime current, DayOfWeek dayOfWeek)
    {
        DateTime dateTime = current.FirstDayOfMonth();
        if (dateTime.DayOfWeek != dayOfWeek)
        {
            dateTime = dateTime.NextWeekDay(dayOfWeek);
        }

        return dateTime;
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
    public static DateTime LastWeekDayOfMonth(this DateTime current, DayOfWeek dayOfWeek)
    {
        int numDays = current.NumberOfDaysInMonth();

        DateTime date;

        do
        {
            date = new DateTime(current.Year, current.Month, numDays);
            numDays--;
        } while (date.DayOfWeek != dayOfWeek);

        return date;
    }
    
    public static DateTime StartOfWeek(DayOfWeek startOfWeek)
    {
        var current = DateTime.UtcNow;
        var diff = (7 + (current.DayOfWeek - startOfWeek)) % 7;
        return current.AddDays(-1 * diff).Date;
    }

    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        var diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
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
    public static DateTime NextWeekDay(this DateTime current, DayOfWeek dayOfWeek)
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