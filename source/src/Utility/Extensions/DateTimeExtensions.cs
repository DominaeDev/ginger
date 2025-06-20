using System;
using System.Globalization;

namespace Ginger
{
	public static class DateTimeExtensions
	{
		public static DateTime FromUnixTime(long unixTime)
		{
			try
			{
				if (unixTime == 0) // Undefined
					return DateTime.Now;
				else if (unixTime > 999999999999) // In milliseconds (probably)
					return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime.ToLocalTime();
				else // In seconds
					return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime.ToLocalTime();
			}
			catch
			{
				return DateTime.UtcNow;
			}
		}

		public static long ToUnixTimeMilliseconds(this DateTime dateTime)
		{
			return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
		}

		public static long ToUnixTimeSeconds(this DateTime dateTime)
		{
			return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds() / 1000L;
		}

		public static string ToISO8601(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffK");
		}

		public static DateTime Max(DateTime a, DateTime b)
		{
			return a > b ? a : b;
		}

		public static string ToTavernDate(this DateTime dateTime)
		{
			string date = dateTime.ToString("yyyy-MM-dd");
			string hour = dateTime.ToString("HH");
			string minute = dateTime.ToString("mm");
			string second = dateTime.ToString("ss");
			string millisecond = dateTime.ToString("fff");
			return string.Concat(date, " @", hour, "h ", minute, "m ", second, "s ", millisecond, "ms");
		}

		public static DateTime FromTavernDate(string tavernDate)
		{
			if (string.IsNullOrEmpty(tavernDate))
				return DateTime.Now;
			DateTime tryDate;
			if (DateTime.TryParse(tavernDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out tryDate))
				return tryDate;

			int idx_at = tavernDate.IndexOf('@');
			if (idx_at != -1)
			{
				DateTime date, time;
				DateTime.TryParse(tavernDate.Substring(0, idx_at), out date);

				string sTime = tavernDate.Substring(idx_at + 1);
				sTime = sTime.Replace("h", ":");
				sTime = sTime.Replace("m", ":");
				sTime = sTime.Replace("s", ".");
				sTime = sTime.Replace(" ", "");
				int idx_ms = sTime.IndexOf('.');
				if (idx_ms != -1)
					sTime = sTime.Substring(0, idx_ms);
				DateTime.TryParse(sTime, out time);
				return (date.Date + time.TimeOfDay).ToLocalTime();
			}
			else
			{
				long unixTime;
				if (long.TryParse(tavernDate, out unixTime))
					return FromUnixTime(unixTime).ToLocalTime();
			}

			return DateTime.Now;
		}
	}
}
