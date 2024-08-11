using System;
using System.Globalization;
using System.Reflection;

namespace Ginger
{
	public static class AppVersion
	{
		public static string ProductTitle
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "")
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

		public static string ProductVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
			}
		}

		public static string ProductName
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
#if BUILD_X64
				return ((AssemblyProductAttribute)attributes[0]).Product + " (64-bit)";
#else
				return ((AssemblyProductAttribute)attributes[0]).Product + " (32-bit)";
#endif
			}
		}

		/*
		public string BuildTime
		{
			get
			{
				var version = Assembly.GetEntryAssembly().GetName().Version;
				var buildDateTime = new DateTime(2000, 1, 1).Add(new TimeSpan(
					TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
					TimeSpan.TicksPerSecond * 2 * version.Revision));

				return buildDateTime.ToString("d", CultureInfo.CurrentCulture);
			}
		}
		*/
	}
}
