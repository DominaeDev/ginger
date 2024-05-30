using System;
using System.Collections.Generic;

namespace LlamaTokenizer
{
	public class PriorityQueue<T> where T : IComparable<T>
	{
		private List<T> _heap;
	
		public PriorityQueue()
		{
			_heap = new List<T>();
		}

		public int size()
		{
			return _heap.Count;
		}

		public bool isEmpty()
		{
			return _heap.Count == 0;
		}

		public T peek()
		{
			return _heap[0];
		}

		public int push(T value)
		{
			_heap.Add(value);
			_siftUp();
			return size();
		}

		public T pop()
		{
			T poppedValue = peek();
			int bottom = size() - 1;
			if (bottom > 0)
			{
				_swap(0, bottom);
			}
			_heap.RemoveAt(bottom);
			_siftDown();
			return poppedValue;
		}

		private int _parent(int i)
		{
			return (int)((uint)(i + 1) >> 1) - 1;
		}

		private int _left(int i)
		{
			return (i << 1) + 1;
		}

		private int _right(int i)
		{
			return (i + 1) << 1;
		}

		private bool _greater(int i, int j)
		{
			return _heap[i].CompareTo(_heap[j]) <= 0;
		}

		private void _swap(int i, int j)
		{
			T tmp = _heap[i];
			_heap[i] = _heap[j];
			_heap[j] = tmp;
		}

		private void _siftUp()
		{
			int node = size() - 1;
			while (node > 0 && _greater(node, _parent(node)))
			{
				_swap(node, _parent(node));
				node = _parent(node);
			}
		}

		private void _siftDown()
		{
			int node = 0;
			while (
				(_left(node) < size() && _greater(_left(node), node)) ||
				(_right(node) < size() && _greater(_right(node), node))
			)
			{
				int maxChild = (_right(node) < size() && _greater(_right(node), _left(node))) ? _right(node) : _left(node);
				_swap(node, maxChild);
				node = maxChild;
			}
		}
	}
}
