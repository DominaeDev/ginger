using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public static class Lorebooks
	{
		public static List<Lorebook> books = new List<Lorebook>();

		public static void LoadLorebooks()
		{
			books.Clear();

			var lorebookPath = Utility.ContentPath("Lorebooks");

			var files = Utility.FindFilesInFolder(lorebookPath, "*.json", true);
			for (int i = 0; i < files.Length; ++i)
			{
				var lorebook = new Lorebook() {
					filename = files[i],
					paths = Utility.SplitPath(Utility.GetSubpath(lorebookPath, files[i]) ?? ""),
				};
				int nErrors;
				if (lorebook.LoadFromJson(files[i], out nErrors) == Lorebook.LoadError.NoError)
					books.Add(lorebook);
			}

			var csvFiles = Utility.FindFilesInFolder(lorebookPath, "*.csv", true);
			for (int i = 0; i < csvFiles.Length; ++i)
			{
				var lorebook = new Lorebook() {
					filename = csvFiles[i],
					paths = Utility.SplitPath(Utility.GetSubpath(lorebookPath, files[i]) ?? ""),
				};
				if (lorebook.LoadFromCsv(csvFiles[i]))
					books.Add(lorebook);
			}
		}

		public static string[] GetFolders(string parent)
		{
			string[] path = Utility.SplitPath(parent);

			Func<string[], string[], bool> fnBeginsWith = (r, sub) => {
				if (r.Length >= sub.Length)
					return false;
				for (int i = 0; i < r.Length; ++i)
					if (string.Compare(r[i], sub[i], true) != 0)
						return false;
				return true;
			};
							
			return books
				.Where(b => fnBeginsWith(path, b.paths))
				.Select(r => r.paths[path.Length])
				.DistinctBy(p => p.ToLowerInvariant())
				.OrderBy(p => p)
				.ToArray();
		}

		public static Lorebook[] GetLorebooksInFolder(string parent)
		{
			string[] path = Utility.SplitPath(parent);

			Func<string[], string[], bool> fnExact = (a, b) => {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
					if (string.Compare(a[i], b[i], true) != 0)
						return false;
				return true;
			};
				
			return books.Where(l => fnExact(l.paths, path))
				.OrderBy(l => l.name)
				.ToArray();
		}
	}

}
