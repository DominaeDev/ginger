using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public static class EnumAttributeHelper<T> where T : struct, IConvertible
	{
		static EnumAttributeHelper()
		{
			var enumType = typeof(T);
			var values = EnumInfo<T>.GetValues();
			foreach (var value in values)
			{
				string valueName = Enum.GetName(enumType, value);
				var field = enumType.GetField(valueName);
				var customAttributes = field.GetCustomAttributes(false);
				if (customAttributes != null)
					ms_CustomAttributes.TryAdd(value, customAttributes);
			}
		}

		private static readonly Dictionary<T, object[]> ms_CustomAttributes = new Dictionary<T, object[]>();

		public static object[] GetAttributes(T value)
		{
			object[] values;
			if (ms_CustomAttributes.TryGetValue(value, out values))
				return values;
			return new object[0];
		}

		public static U[] GetAttributes<U>(T value)
		{
			object[] values;
			if (ms_CustomAttributes.TryGetValue(value, out values))
				return values.OfType<U>().ToArray();
			return new U[0];
		}

		public static U GetAttribute<U>(T value) where U : System.Attribute
		{
			return GetAttributes(value)
				.OfType<U>()
				.FirstOrDefault();
		}

		public static bool HasAttribute<U>(T value) where U : System.Attribute
		{
			return GetAttributes(value)
				.OfType<U>()
				.IsEmpty() == false;
		}
	}
}
