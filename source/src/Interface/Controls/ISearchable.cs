namespace Ginger
{
	public interface ISearchable
	{
		int Find(string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1);
		int SelectionStart { get; }
		void Select(int start, int length);
		bool Enabled { get; }
	}

	public interface ISearchableContainer
	{
		ISearchable[] GetSearchables();
		bool Enabled { get; }
	}

	public struct Searchable 
	{
		public RecipePanel panel;
		public ISearchable control;
	}
}
