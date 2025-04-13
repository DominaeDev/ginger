using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ginger
{
	public static class ListExtensions
	{
		public static int IndexOfAny<TSource>(this IList<TSource> source, Func<TSource, bool> predicate)
		{
			for (int i = 0; i < source.Count; ++i)
			{
				if (predicate.Invoke(source[i]))
					return i;
			}
			return -1;
		}

		public static int IndexOfAny<TSource>(this IList<TSource> source, TSource[] options)
		{
			foreach (var option in options)
			{
				int index = source.IndexOf(option);
				if (index != -1)
					return index;
			}
			return -1;
		}

		public static bool Swap<TSource>(this IList<TSource> source, int a, int b)
		{
			if (source == null || a < 0 || b < 0 || a >= source.Count || b >= source.Count)
				return false;

			TSource tmp = source[a];
			source[a] = source[b];
			source[b] = tmp;
			return true;
		}

		public static int Remove<TSource>(this IList<TSource> source, IEnumerable<TSource> list)
		{
			int count = 0;
			TSource[] items = list.ToArray();
			for (int i = 0; i < items.Length; ++i)
			{
				if (source.Remove(items[i]))
					++count;
			}
			return count;
		}

		public static bool IsEmpty<T>(this T[] array)
		{
			return array == null || array.Length == 0;
		}
	}
}
