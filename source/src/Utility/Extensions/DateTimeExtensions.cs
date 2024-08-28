using System;

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
	}
}
