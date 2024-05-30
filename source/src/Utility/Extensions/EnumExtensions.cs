using System;

namespace Ginger
{
	public static class EnumerationExtensions
	{
		public static bool Contains<T>(this Enum enumValue, T flag) where T : struct, IConvertible
		{
			if (enumValue.GetType() != typeof(T))
				return false;

			if (EnumHelper.ToInt(flag) == 0)
				return false;

			try
			{
				return EnumHelper.Contains((T)Convert.ChangeType(enumValue, typeof(T)), flag);
			}
			catch
			{
				return false;
			}
		}

		public static bool ContainsAny<T>(this Enum enumValue, T flag) where T : struct, IConvertible
		{
			if (enumValue.GetType() != typeof(T))
				return false;

			try
			{
				Type underlyingType = EnumInfo<T>.UnderlyingType;
				if (underlyingType == typeof(ulong))
					return (Convert.ToUInt64(enumValue) & Convert.ToUInt64(flag)) != 0UL;
				if (underlyingType == typeof(uint))
					return (Convert.ToUInt32(enumValue) & Convert.ToUInt32(flag)) != 0U;
				if (underlyingType == typeof(int))
					return (Convert.ToInt32(enumValue) & Convert.ToInt32(flag)) != 0;
				if (underlyingType == typeof(Int64))
					return (Convert.ToInt64(enumValue) & Convert.ToInt64(flag)) != 0L;
			}
			catch
			{
				return false;
			}
			return false;
		}
	}
}