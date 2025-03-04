using System;
using System.Linq;

namespace Ginger
{
	public class Undo : IHasUndoStack<Undo.UndoState>
	{
		public enum Kind
		{
			Undefined = 0,
			Parameter,	// Parameter changed
			RecipeAddRemove, // Add / Remove recipes
			RecipeOrder, // Same recipes, different order
			RecipeList, // Replace recipes
		}

		public struct UndoState
		{
			public StringHandle actionId;
			public string actionName;

			// State
			public Kind kind;
			public CardData card;
			public CharacterData[] characters;
			public int selectedCharacter;
			public Integration.Backyard.Link link;

			// Generation settings
			public bool autoConvertNames;
			public string userPlaceholder;

			public bool IsEmpty()
			{
				return characters == null;
			}
		}

		public class UndoRedoEventArgs : EventArgs
		{
			public Kind kind;
			public UndoState undoState;
		}

		public static EventHandler<UndoRedoEventArgs> OnUndoRedo;
		private static Undo _instance;

		private UndoStack<UndoState> _undoStack;
		private bool _isActive = true;

		public static bool canUndo
		{
			get
			{
				return _instance != null && _instance._undoStack.undoCount > 1;
			}
		}

		public static bool canRedo
		{
			get
			{
				return _instance != null && _instance._undoStack.redoCount > 0;
			}
		}
		
		public static void Initialize()
		{
			_instance = new Undo();
		}

		public Undo()
		{
			_undoStack = new UndoStack<UndoState>(this, Math.Max(AppSettings.Settings.UndoSteps, 10));
			InitUndo();
		}

		public void InitUndo()
		{
			_isActive = true;
			_undoStack.Clear();
			PushUndoState(Kind.Undefined, "");
		}

		public void OnUndoState(UndoState state)
		{
			Current.Card = state.card.Clone();
			Current.Characters = state.characters.Select(c => c.Clone()).ToList();
			Current.SelectedCharacter = state.selectedCharacter;
			Current.Link = state.link?.Clone();

			AppSettings.Settings.AutoConvertNames = state.autoConvertNames;
			AppSettings.Settings.UserPlaceholder = state.userPlaceholder;

			Kind kind = _undoStack.PeekRedo().kind; // Previous state

			OnUndoRedo?.Invoke(this, new UndoRedoEventArgs() {
				kind = kind,
				undoState = state,
			});
		}

		public void OnRedoState(UndoState state)
		{
			Current.Card = state.card.Clone();
			Current.Characters = state.characters.Select(c => c.Clone()).ToList();
			Current.SelectedCharacter = state.selectedCharacter;
			Current.Link = state.link?.Clone();

			AppSettings.Settings.AutoConvertNames = state.autoConvertNames;
			AppSettings.Settings.UserPlaceholder = state.userPlaceholder;

			OnUndoRedo?.Invoke(this, new UndoRedoEventArgs() {
				kind = state.kind,
				undoState = state,
			});
		}

		private void PushUndoState(Kind kind, string actionName, StringHandle actionId = default(StringHandle))
		{
			if (StringHandle.IsNullOrEmpty(actionId) == false)
			{
				UndoState prev = _undoStack.PeekUndo();
				if (prev.actionId == actionId)
					_undoStack.Pop(); // Overwrite same action
			}

			UndoState state = CreateUndoState();
			state.kind = kind;
			state.actionId = actionId;
			state.actionName = actionName;
			_undoStack.PushState(state);
		}

		private static UndoState CreateUndoState()
		{
			var state = new UndoState() {
				characters = Current.Characters.Select(c => c.Clone()).ToArray(),
				selectedCharacter = Current.SelectedCharacter,
				card = Current.Card.Clone(),
				autoConvertNames = AppSettings.Settings.AutoConvertNames,
				userPlaceholder = AppSettings.Settings.UserPlaceholder,
				link = Current.Link?.Clone(),
			};
			return state;
		}

		public static void Push(Kind kind, string actionName, StringHandle actionId = default(StringHandle))
		{
			if (_instance != null && _instance._isActive)
				_instance.PushUndoState(kind, actionName, actionId);
		}

		public static void DoUndo()
		{
			if (_instance != null)
			{
				_instance._isActive = false;
				_instance._undoStack.Undo();
				_instance._isActive = true;
			}
		}

		public static void DoRedo()
		{
			if (_instance != null)
			{
				_instance._isActive = false;
				_instance._undoStack.Redo();
				_instance._isActive = true;
			}
		}

		public static void Clear()
		{
			if (_instance != null)
				_instance.InitUndo();
		}

		public static UndoState PeekUndo()
		{
			if (_instance != null)
				return _instance._undoStack.PeekUndo();
			return default(UndoState);
		}

		public static UndoState PeekRedo()
		{
			if (_instance != null)
				return _instance._undoStack.PeekRedo();
			return default(UndoState);
		}

		public static void Suspend()
		{
			if (_instance != null)
				_instance._isActive = false;
		}

		public static void Resume()
		{
			if (_instance != null)
				_instance._isActive = true;
		}
	}
}
