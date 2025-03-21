using System;
using System.Collections;
using System.Collections.Generic;

namespace Ginger
{
	public class HashTable<K, T> : IDictionary<K, IList<T>>
	{
		private Dictionary<K, List<T>> _dict = new Dictionary<K, List<T>>();

		public IList<T> this[K key]
		{
			get { return _dict[key]; }
			set { Add(key, value); }
		}

		public ICollection<K> Keys => _dict.Keys;

		public ICollection<IList<T>> Values => _dict.Values as ICollection<IList<T>>;

		public int Count => _dict.Count;

		public bool IsReadOnly => throw new NotImplementedException();

		public void Add(K key, T value)
		{
			if (_dict.ContainsKey(key) == false)
				_dict.Add(key, new List<T>() { value });
			else
				_dict[key].Add(value);
		}

		public void Add(K key, IList<T> values)
		{
			if (_dict.ContainsKey(key) == false)
				_dict.Add(key, new List<T>(values));
			else
				_dict[key].AddRange(values);
		}

		public void Add(KeyValuePair<K, IList<T>> item)
		{
			this.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			_dict.Clear();
		}

		public bool Contains(KeyValuePair<K, IList<T>> item)
		{
			return _dict.ContainsKey(item.Key)
				&& _dict[item.Key] == item.Value;
		}

		public bool ContainsKey(K key)
		{
			return _dict.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<K, IList<T>>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(K key)
		{
			return _dict.Remove(key);
		}

		public bool Remove(KeyValuePair<K, IList<T>> item)
		{
			if (_dict.ContainsKey(item.Key) == false)
				return false;

			return _dict[item.Key].Remove(item.Value) != 0;
		}

		public bool TryGetValue(K key, out IList<T> value)
		{
			List<T> list;
			if (_dict.TryGetValue(key, out list))
			{
				value = list;
				return true;
			}

			value = default(IList<T>);
			return false;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		public IEnumerator<KeyValuePair<K, IList<T>>> GetEnumerator()
		{
			return _dict.GetEnumerator() as IEnumerator<KeyValuePair<K, IList<T>>>;
		}
	}
}
