using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public interface IHasUndoStack<T>
	{
		void InitUndo();
		void OnUndoState(T state);
		void OnRedoState(T state);
	}

	public class UndoStack<T> 
	{
		private const int MAX_UNDO = 250;

		public int UndoLimit = 0;
		private Stack<T> _undoStates;
		private Stack<T> _redoStates;
		private IHasUndoStack<T> _owner;
		private bool _bIgnoreEvents = false;
		
		public int undoCount { get { return _undoStates.Count; } }
		public int redoCount { get { return _redoStates.Count; } }

		public UndoStack(IHasUndoStack<T> owner, int capacity = 50)
		{
			_owner = owner;
			_undoStates = new Stack<T>(capacity);
			_redoStates = new Stack<T>(capacity);
			UndoLimit = capacity;
		}

		public bool Undo()
		{
			if (_undoStates.Count <= 1)
				return false;

			_bIgnoreEvents = true;
			_redoStates.Push(_undoStates.Pop());
			_owner.OnUndoState(_undoStates.Peek());
			_bIgnoreEvents = false;
			return true;
		}

		public bool Redo()
		{
			if (_redoStates.Count == 0)
				return false;

			_bIgnoreEvents = true;

			var nextState = _redoStates.Pop();
			_undoStates.Push(nextState);
			_owner.OnRedoState(_undoStates.Peek());
			_bIgnoreEvents = false;
			return true;
		}

		public void PushState(T state)
		{
			if (_bIgnoreEvents)
				return;

			if (_undoStates.Count > 0 && EqualityComparer<T>.Default.Equals(_undoStates.Peek(), state))
				return;

			_undoStates.Push(state);
			_redoStates.Clear();

			// Trim stack by max length
			if (UndoLimit > 0 && (_undoStates.Count > UndoLimit || _undoStates.Count > MAX_UNDO))
			{
				int maxLength = Math.Min(UndoLimit, MAX_UNDO);
				_undoStates = new Stack<T>(_undoStates.Take(maxLength).Reverse());
			}
		}

		public T Pop()
		{
			if (_undoStates.Count > 0)
				return _undoStates.Pop();
			return default(T);
		}

		public void Clear()
		{
			_undoStates.Clear();
			_redoStates.Clear();
		}

		public T PeekUndo()
		{
			if (_undoStates.Count > 0)
				return _undoStates.Peek();
			return default(T);
		}

		public T PeekRedo()
		{
			if (_redoStates.Count > 0)
				return _redoStates.Peek();
			return default(T);
		}
	}

}
