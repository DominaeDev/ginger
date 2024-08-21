using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ginger
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class HideEnumAttribute : System.Attribute
	{
	}

	public static class EnumInfo<T> where T : struct, IConvertible
	{
		static EnumInfo()
		{
			Type type = typeof(T);

			UnderlyingType = Enum.GetUnderlyingType(type);

			var names = Enum.GetNames(type);

			var ignore = new HashSet<string>(
				names.Where(name => {
					var field = type.GetField(name);
					return field.GetCustomAttributes(typeof(HideEnumAttribute), false).Length > 0;
				}));

			ms_EnumToNameLookup = new Dictionary<T, string>();
			foreach (var name in names.Except(ignore))
				ms_EnumToNameLookup.TryAdd((T)Enum.Parse(type, name), name);

			ms_NameToEnumLookup = new Dictionary<string, T>();
			foreach (var kvp in ms_EnumToNameLookup)
				ms_NameToEnumLookup.TryAdd(kvp.Value.ToLowerInvariant(), kvp.Key);

			// Values
			ms_Values = new ReadOnlyCollection<T>(Enum.GetValues(type).OfType<T>().Distinct().ToArray());

			if (UnderlyingType == typeof(long))	//	64 bit bit enum
			{
				ms_ValueLookup = ms_Values
					.ToDictionary(x => (long)(object)x, x => x);
			}
			else if (UnderlyingType == typeof(int))	// 32 bit enum
			{
				ms_ValueLookup = ms_Values
					.ToDictionary(x => (long)(int)(object)x, x => x);
			}
			else if (UnderlyingType == typeof(short))	// 16 bit enum
			{
				ms_ValueLookup = ms_Values
					.ToDictionary(x => (long)(short)(object)x, x => x);
			}
			else if (UnderlyingType == typeof(byte))	// 8 bit enum
			{
				ms_ValueLookup = ms_Values
					.ToDictionary(x => (long)(byte)(object)x, x => x);
			}
			else
			{
				throw new ArgumentException("Unsupported underlying enum type.");
			}
		}

		// Static lookup tables
		private static readonly Dictionary<string, T> ms_NameToEnumLookup;
		private static readonly Dictionary<T, string> ms_EnumToNameLookup;
		private static readonly Dictionary<long, T> ms_ValueLookup;
		private static readonly ReadOnlyCollection<T> ms_Values;
		public static readonly Type UnderlyingType;

		public static bool Convert(string string_value, out T result)
		{
			if (ms_NameToEnumLookup.TryGetValue(string_value.ToLowerInvariant(), out result))
				return true;
			return false;
		}

		public static bool Convert(StringHandle string_value, out T result)
		{
			if (ms_NameToEnumLookup.TryGetValue(string_value.ToString(), out result))
				return true;
			return false;
		}

		public static bool Convert(long value, out T result)
		{
			return ms_ValueLookup.TryGetValue(value, out result);
		}

		public static string ToString(T value)
		{
			return GetName(value);
		}

		public static string GetName(T value)
		{
			string result;
			if (ms_EnumToNameLookup.TryGetValue(value, out result))
				return result;
			return Enum.GetName(typeof(T), value);
		}

		public static ReadOnlyCollection<T> GetValues()
		{
			return ms_Values;
		}
	}

	public static class EnumHelper
	{
		public static string ToString<T>(T e) where T : struct, IConvertible
		{
			return EnumInfo<T>.ToString(e);
		}

		public static int ToInt(Enum e)
		{
			return Convert.ToInt32(e);
		}

		public static int ToInt<T>(T e) where T : struct, IConvertible
		{
			return Convert.ToInt32(e);
		}
		
		public static long ToLong<T>(Enum e) where T : struct, IConvertible
		{
			return Convert.ToInt64(e);
		}

		public static long ToLong<T>(T e) where T : struct, IConvertible
		{
			return Convert.ToInt64(e);
		}

		public static int Compare(Enum a, Enum b)
		{
			return Convert.ToInt32(a) - Convert.ToInt32(b);
		}

		public static T FromString<T>(string enumName, T default_value = default(T)) where T : struct, IConvertible
		{
			if (string.IsNullOrEmpty(enumName))
				return default_value;

			T result;
			if (EnumInfo<T>.Convert(enumName, out result))
				return result;
			return default_value;
		}

		public static T FromInt<T>(int enumValue, T default_value = default(T)) where T : struct, IConvertible
		{
			T result;
			if (EnumInfo<T>.Convert(enumValue, out result))
				return result;

			if (Enum.TryParse(enumValue.ToString(), out result))
				return result;

			return default_value;
		}

		public static T FromLong<T>(long enumValue, T default_value = default(T)) where T : struct, IConvertible
		{
			T result;
			if (EnumInfo<T>.Convert(enumValue, out result))
				return result;
			return default_value;
		}

		public static bool Increment<T>(ref T enumValue, int increment, T enumMin, T enumMax) where T : struct, IConvertible
		{
			int iValue = (int)(object)enumValue + increment;

			// Clamp
			if ((int)(object)enumMin > iValue)
			{
				enumValue = enumMin;
				return true;
			}
			else if ((int)(object)enumMax < iValue)
			{
				enumValue = enumMax;
				return true;
			}

			T newEnumValue;
			if (EnumInfo<T>.Convert(iValue, out newEnumValue) == false)
				return false;

			enumValue = newEnumValue;
			return true;
		}

		public static T Increment<T>(T enumValue, int increment, T enumMin, T enumMax) where T : struct, IConvertible
		{
			int iValue = (int)(object)enumValue + increment;

			// Clamp
			if ((int)(object)enumMin > iValue)
				return enumMin;
			else if ((int)(object)enumMax < iValue)
				return enumMax;

			T newEnumValue;
			if (EnumInfo<T>.Convert(iValue, out newEnumValue) == false)
				return enumValue;

			return newEnumValue;
		}

		public static bool Contains<T>(T enumValue, T flag) where T : struct, IConvertible
		{
			try
			{
				Type underlyingType = EnumInfo<T>.UnderlyingType;

				if (underlyingType == typeof(ulong))
					return (Convert.ToUInt64(enumValue) & Convert.ToUInt64(flag)) == Convert.ToUInt64(flag);
				if (underlyingType == typeof(uint))
					return (Convert.ToUInt32(enumValue) & Convert.ToUInt32(flag)) == Convert.ToUInt32(flag);
				if (underlyingType == typeof(int))
					return (Convert.ToInt32(enumValue) & Convert.ToInt32(flag)) == Convert.ToInt32(flag);
				if (underlyingType == typeof(Int64))
					return (Convert.ToInt64(enumValue) & Convert.ToInt64(flag)) == Convert.ToInt64(flag);
			}
			catch
			{
				return false;
			}
			return false;
		}

		public static void Toggle<T>(ref T enumValue, T flag, bool toggled) where T : struct, IConvertible
		{
			if (toggled)
				enumValue = (T)Enum.Parse(typeof(T), (Convert.ToInt64(enumValue) | Convert.ToInt64(flag)).ToString());
			else
				enumValue = (T)Enum.Parse(typeof(T), (Convert.ToInt64(enumValue) & ~Convert.ToInt64(flag)).ToString());
		}
	}
}
