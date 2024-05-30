using System;

namespace Ginger
{
	public static class DateTimeExtensions
	{
		public static long ToUnixTimeMilliseconds(this DateTime dateTime)
		{
			return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
		}

		public static DateTime FromUnixTimeMilliseconds(long unixTime)
		{
			try
			{
				return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
			}
			catch
			{
				return DateTime.UtcNow;
			}
		}
	}
}
