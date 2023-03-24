using System.Globalization;

namespace System
{
    public static class DateTimeExtensions
    {
        public static string GetMonthName(this DateTime date, CultureInfo culture = null)
        {
	        if (culture == null) culture = CultureInfo.CurrentCulture;
			

            return culture.DateTimeFormat.GetMonthName(date.Month);
        }

        public static IEnumerable<DateTime> GetEarlierDates(this DateTime initialDate, int interval)
        {
            var resultado = new DateTime[interval];

            for (var i = 0; i < interval; i++)
                resultado[i] = initialDate.AddMonths(-i);

            return resultado;
        }

        public static int GetAge(this DateTime date)
        {
            var age = DateTime.Now.Year - date.Year;
            if (DateTime.Now.Month < date.Month || (DateTime.Now.Month == date.Month && DateTime.Now.Day < date.Day))
                age--;
            return age;
        }


        public static DateTime Truncate(this DateTime dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
        }

        public static DateTime? Truncate(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return null;

            return Truncate(dateTime.Value);
        }

        public static bool Between(this DateTime input, DateTime date1, DateTime date2)
        {
            return (input > date1 && input < date2);
        }

        /// <summary>
        /// Gets the 12:00:00 instance of a DateTime
        /// </summary>
        public static DateTime AbsoluteStart(this DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// Gets the 11:59:59 instance of a DateTime
        /// </summary>
        public static DateTime AbsoluteEnd(this DateTime dateTime)
        {
            return AbsoluteStart(dateTime).AddDays(1).AddMinutes(-1);
        }

        public static DateTime[] GetDatesUntil(this DateTime start, DateTime end)
        {
            return Enumerable.Range(0, 1 + end.Subtract(start).Days)
                      .Select(offset => start.AddDays(offset))
                      .ToArray();
        }
    }
}
