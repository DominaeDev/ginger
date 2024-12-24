using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ginger
{
	public static class MRUList
	{
		public struct MRUItem
		{
			public string filename;
			public string characterName;
		}

		public static Queue<MRUItem> mruItems = new Queue<MRUItem>();
		public static int MaxCount = 30;

		public static void AddToMRU(string filename, string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				characterName = Path.GetFileNameWithoutExtension(filename);

			RemoveFromMRU(filename);

			mruItems.Enqueue(new MRUItem() {
				filename = filename,
				characterName = characterName,
			});

			while (mruItems.Count > MaxCount)
				mruItems.Dequeue();
		}

		public static void RemoveFromMRU(string filename)
		{
			mruItems = new Queue<MRUItem>(mruItems.Where(i => string.Compare(i.filename, filename, true) != 0));
		}

		public static void Clear()
		{
			mruItems.Clear();
		}
	}
}
