using System.Collections.Generic;

namespace Ginger
{
	static class DictionaryExtensions
	{
		public static void Set<K, T>(this Dictionary<K, T> dictionary, K key, T value)
		{
			if (dictionary.ContainsKey(key))
				dictionary[key] = value;
			else
				dictionary.Add(key, value);
		}

		public static bool TryAdd<K, T>(this Dictionary<K, T> dictionary, K key, T value)
		{
			if (dictionary.ContainsKey(key) == false)
			{
				dictionary.Add(key, value);
				return true;
			}
			return false;
		}
	}
}
