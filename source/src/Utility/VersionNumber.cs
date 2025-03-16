using System;
using System.Globalization;

namespace Ginger
{
	public struct VersionNumber : IComparable
	{
		public int Major;
		public int Minor;
		public int Build;

		public VersionNumber(int major, int minor, int build = 0)
		{
			Major = major;
			Minor = minor;
			Build = build;
		}

		public bool isDefined { get { return Major != 0 || Minor != 0 || Build != 0; } }

		public static VersionNumber Parse(string value)
		{
			int iMajor = 0;
			int iMinor = 0;
			int iBuild = 0;

			if (string.IsNullOrEmpty(value) == false)
			{
				int pos_minor = value.IndexOf('.');
				if (pos_minor != -1)
				{
					int pos_build = value.IndexOf('.', pos_minor + 1);
					if (pos_build != -1)
					{
						int.TryParse(value.Substring(0, pos_minor), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
						int.TryParse(value.Substring(pos_minor + 1, pos_build - pos_minor - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMinor);
						int.TryParse(value.Substring(pos_build + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iBuild);
					}
					else
					{
						int.TryParse(value.Substring(0, pos_minor), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
						int.TryParse(value.Substring(pos_minor + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMinor);
					}
				}
				else
				{
					int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
				}
			}

			return new VersionNumber()
			{
				Major = iMajor,
				Minor = iMinor,
				Build = iBuild,
			};
		}

		public static VersionNumber ParseInside(string text)
		{
			char[] digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

			if (string.IsNullOrEmpty(text))
				return Zero;

			int firstDigit = text.IndexOfAny(digits, 0);
			if (firstDigit == -1)
				return Zero;

			string version;
			int lastDigit = text.FindIndex(firstDigit, c => !(c == '.' || char.IsDigit(c)));
			if (lastDigit != -1)
				return Parse(text.Substring(firstDigit, lastDigit - firstDigit));
			else
				return Parse(text.Substring(firstDigit));
		}

		public static bool TryParse(string value, out VersionNumber version)
		{
			int iMajor = 0;
			int iMinor = 0;
			int iBuild = 0;

			if (string.IsNullOrEmpty(value) == false)
			{
				int pos_minor = value.IndexOf('.');
				if (pos_minor != -1)
				{
					int pos_build = value.IndexOf('.', pos_minor + 1);
					if (pos_build != -1)
					{
						int.TryParse(value.Substring(0, pos_minor), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
						int.TryParse(value.Substring(pos_minor + 1, pos_build - pos_minor - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMinor);
						int.TryParse(value.Substring(pos_build + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iBuild);
					}
					else
					{
						int.TryParse(value.Substring(0, pos_minor), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
						int.TryParse(value.Substring(pos_minor + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out iMinor);
					}
				}
				else
				{
					int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out iMajor);
				}
			}

			if (iMajor != 0 || iMinor != 0 || iBuild != 0)
			{
				version = new VersionNumber()
				{
					Major = iMajor,
					Minor = iMinor,
					Build = iBuild,
				};
				return true;
			}
			else
			{
				version = default(VersionNumber);
				return false;
			}
		}

		public override string ToString()
		{
			if (Build == 0)
				return string.Format("{0}.{1}", Major, Minor);
			return string.Format("{0}.{1}.{2}", Major, Minor, Build);
		}

		public string ToFullString()
		{
			return string.Format("{0}.{1}.{2}", Major, Minor, Build);
		}

		public decimal ToDecimal()
		{
			return decimal.Parse(string.Format("{0}.{1}", Major, Minor), NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		public static bool operator <(VersionNumber a, VersionNumber b)
		{
			if (a.Major == b.Major)
			{
				if (a.Minor == b.Minor)
					return a.Build < b.Build;
				return a.Minor < b.Minor;
			}
			return a.Major < b.Major;
		}

		public static bool operator >(VersionNumber a, VersionNumber b)
		{
			if (a.Major == b.Major)
			{
				if (a.Minor == b.Minor)
					return a.Build > b.Build;
				return a.Minor > b.Minor;
			}
			return a.Major > b.Major;
		}

		public static bool operator <=(VersionNumber a, VersionNumber b)
		{
			if (a.Major == b.Major)
			{
				if (a.Minor == b.Minor)
					return a.Build <= b.Build;
				return a.Minor <= b.Minor;
			}
			return a.Major <= b.Major;
		}

		public static bool operator >=(VersionNumber a, VersionNumber b)
		{
			if (a.Major == b.Major)
			{
				if (a.Minor == b.Minor)
					return a.Build >= b.Build;
				return a.Minor >= b.Minor;
			}
			return a.Major >= b.Major;
		}

		public static bool operator ==(VersionNumber a, VersionNumber b)
		{
			return a.Major == b.Major
				&& a.Minor == b.Minor
				&& a.Build == b.Build;
		}

		public static bool operator !=(VersionNumber a, VersionNumber b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is VersionNumber))
				return false;

			VersionNumber other = (VersionNumber)obj;
			if ((System.Object)other == null)
				return false;

			return this == other;
		}

		public bool Equals(VersionNumber other)
		{
			if ((object)other == null)
				return false;

			return this == other;
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public int CompareTo(object obj)
		{
			if (obj == null || !(obj is VersionNumber))
				return 1;

			VersionNumber other = (VersionNumber)obj;
			int major = this.Major.CompareTo(other.Major);
			int minor = this.Minor.CompareTo(other.Minor);
			int build = this.Build.CompareTo(other.Build);
			if (major != 0)
				return major;
			if (minor != 0)
				return minor;
			return build;
		}

		public static VersionNumber Zero = new VersionNumber(0, 0, 0);

		public static VersionNumber Application = Parse(AppVersion.ProductVersion);
		
	}

}
