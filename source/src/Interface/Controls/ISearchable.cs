﻿using System.Windows.Forms;

namespace Ginger
{
	public interface ISearchable
	{
		int Find(string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1);
		void FocusAndSelect(int start, int length);
		
		bool Enabled { get; }
		TextBoxBase SearchableControl { get; }
	}

	public interface ISearchableContainer
	{
		ISearchable[] GetSearchables();
		bool Enabled { get; }
	}

	public struct Searchable 
	{
		public RecipePanel panel;
		public ISearchable instance;
	}
}
