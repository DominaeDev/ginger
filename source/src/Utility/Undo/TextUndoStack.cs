using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public interface IHasTextUndoStack<T>
	{
		void InitUndo();
		void OnUndoState(T state, bool redo);
	}

	public class TextUndoStack<T> 
	{
		private const int MAX_UNDO = 1024;

		public int undoCount { get { return _undoStates.Count; } }
		public int redoCount { get { return _redoStates.Count; } }

		public int UndoLimit = 0;
		private Stack<T> _undoStates;
		private Stack<T> _redoStates;
		private IHasTextUndoStack<T> _owner;
		private bool _bIgnoreEvents = false;

		public TextUndoStack(IHasTextUndoStack<T> owner, int capacity = 250)
		{
			_owner = owner;
			_undoStates = new Stack<T>(capacity);
			_redoStates = new Stack<T>(capacity);
		}

		public bool Undo()
		{
			if (_undoStates.Count == 0)
				return false;

			_bIgnoreEvents = true;
			var state = _undoStates.Pop();
			_redoStates.Push(state);
			_owner.OnUndoState(state, false);
			_bIgnoreEvents = false;
			return true;
		}

		public bool Redo()
		{
			if (_redoStates.Count == 0)
				return false;

			_bIgnoreEvents = true;
			var state = _redoStates.Pop();
			_undoStates.Push(state);
			_owner.OnUndoState(state, true);
			_bIgnoreEvents = false;
			return true;
		}

		public bool CanUndo()
		{
			return _undoStates.Count > 0;
		}

		public bool CanRedo()
		{
			return _redoStates.Count > 0;
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
				_undoStates = new Stack<T>(_undoStates.ToArray().Take(maxLength));
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
