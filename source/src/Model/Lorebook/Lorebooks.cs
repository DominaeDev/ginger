using System.Collections.Generic;

namespace Ginger
{
	public static class Lorebooks
	{
		public static List<Lorebook> books = new List<Lorebook>();

		public static void LoadLorebooks()
		{
			books.Clear();

			var files = Utility.FindFilesInFolder(Utility.ContentPath("Lorebooks"), "*.json", true);
			for (int i = 0; i < files.Length; ++i)
			{
				var lorebook = new Lorebook() {
					filename = files[i],
				};
				if (lorebook.LoadFromJson(files[i]))
					books.Add(lorebook);
			}

			var csvFiles = Utility.FindFilesInFolder(Utility.ContentPath("Lorebooks"), "*.csv", true);
			for (int i = 0; i < csvFiles.Length; ++i)
			{
				var lorebook = new Lorebook() {
					filename = csvFiles[i],
				};
				if (lorebook.LoadFromCsv(csvFiles[i]))
					books.Add(lorebook);
			}
		}
	}

}
