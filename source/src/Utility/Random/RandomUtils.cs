using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public static class RandomUtils
	{
		public static byte Byte(this IRandom rng)
		{
			return Convert.ToByte(rng.Int(0, 255, RandomOption.Inclusive));
		}

		// Returns random item from a collection
		public static T Item<T>(this IRandom rng, ICollection<T> list)
		{
			if (list == null || list.Count == 0)
				return default(T);

			return list.ElementAt(rng.Int(0, list.Count, RandomOption.Exclusive));
		}

		public static T Item<T>(this IRandom rng, IEnumerable<T> enumerable)
		{
			if (enumerable == null)
				return default(T);
			int count = enumerable.Count();
			if (count == 0)
				return default(T);
			else if (count == 1)
				return enumerable.First();

			return enumerable.ElementAt(rng.Int(0, count, RandomOption.Exclusive));
		}
		
		public static T Enum<T>(this IRandom rng) where T : struct, IConvertible
		{
			var values = EnumInfo<T>.GetValues();
			return values.ElementAt(rng.Int(0, values.Count, RandomOption.Exclusive));
		}

		public static float Gaussian(this IRandom rng)
		{
			const int k = 5;
			float r = 0.0f;
			for (int i = 0; i < k; ++i)
				r += rng.Float();
			return r / k;
		}

		public static T Choose<T>(this IRandom rng, T a, T b)
		{
			return Roll(rng, 0.5f) ? a : b;
		}

		public static T Choose<T>(this IRandom rng, IList<T> items)
		{
			return rng.Item(items);
		}

		public static T ChooseFrom<T>(this IRandom rng, params T[] items)
		{
			return rng.Item(items);
		}

		public static bool Roll(this IRandom rng, float probability)
		{
			return rng.Float() < probability;
		}

		public static bool Roll(this IRandom rng, int a, int b)
		{
			if (b <= 0)
			{
				rng.Advance();
				return false;
			}

			return rng.Float() < ((float)a / b);
		}
	}
}