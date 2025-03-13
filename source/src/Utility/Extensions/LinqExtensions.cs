using System.Linq;
using System.Collections.Generic;
using System;

namespace Ginger
{
	public static class LinqExtensions
	{
		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			return Equals(source.FirstOrDefault(), default(T));
		}

		public static IEnumerable<TSource> NotNull<TSource>(this IEnumerable<TSource> source)
		{
			return source.Where(x => x != null);
		}

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
					Func<TSource, TKey> keySelector)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			return _(); IEnumerable<TSource> _()
			{
				var knownKeys = new HashSet<TKey>();
				foreach (var element in source)
				{
					if (knownKeys.Add(keySelector(element)))
						yield return element;
				}
			}
		}

		public static bool ContainsAny<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			foreach (var x in source)
			{
				if (predicate.Invoke(x))
					return true;
			}
			return false;
		}

		public static bool ContainsNoneOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			foreach (var x in source)
			{
				if (predicate.Invoke(x))
					return false;
			}

			return true;
		}

		public static bool ContainsAllIn<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other)
		{
			return source.Intersect(other).Count() == other.Count();
		}

		public static bool ContainsAnyIn<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other)
		{
			return source.Intersect(other).IsEmpty() == false;
		}

		public static bool ContainsAnyOf<TSource>(this IEnumerable<TSource> source, params TSource[] items)
		{
			if (items.Length == 0)
				return false;
			else if (items.Length == 1)
				return source.Contains(items[0]);
			return source.Intersect(items).IsEmpty() == false;
		}

		public static bool ContainsNoneIn<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other)
		{
			return source.Intersect(other).IsEmpty();
		}
		
		public static bool Compare<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other)
		{
			if (other.Count() != source.Count())
				return false;
			return source.ContainsAllIn(other);
		}

		public static TSource TryAggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
		{
			if (source.IsEmpty())
				return default(TSource);
			if (source.Count() == 1)
				return source.First();
			return source.Aggregate(func);
		}

		public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
		{
			if (source == null)
				return true;

			return source.GetEnumerator().MoveNext() == false;
		}
		
		public static IEnumerable<Tuple<T, T>> Pairwise<T>(this IEnumerable<T> source)
		{
			T a;
			T b = default(T);
			
			int index = 0;
			foreach (T element in source)
			{
				a = b;
				b = element;
				if (++index == 2)
				{
					yield return Tuple.Create(a, b);
					index = 0;
				}
			}
			if (index % 2 != 0)
				yield return Tuple.Create(b, default(T));
		}


		public static IList<T> Shuffle<T>(this IList<T> list, IRandom randomizer = null)
		{
			if (randomizer == null)
				randomizer = new RandomNoise();

			if (list.Count <= 1)
				return list;

			int n = list.Count;
			while (n > 1)
			{  
				n--;  
				int k = randomizer.Int(0, n, RandomOption.Inclusive);
				// Swap 
				T value = list[k];  
				list[k] = list[n];  
				list[n] = value;  
			}
			return list;
		}
	}
}
