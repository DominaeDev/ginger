using System;
using System.Collections.Generic;
using System.IO;

namespace Ginger
{
	public static class Dictionaries
	{
		public static IEnumerable<KeyValuePair<string, string>> dictionaries { get { return _dictionaries; } }
		private static Dictionary<string, string> _dictionaries = new Dictionary<string, string>();

		public static bool IsOk()
		{
			return _dictionaries.Count > 0;
		}

		public static bool Load()
		{
			// Read recipes
			var dicFiles = Utility.FindFilesInFolder(Utility.AppPath("Dictionaries"), "*.dic", false);
			for (int i = 0; i < dicFiles.Length; ++i)
			{
				if (Path.GetFileName(dicFiles[i]).ToLowerInvariant() == "user.dic")
					continue; // Ignore user dictionary

				string affFile = Path.Combine(Path.GetDirectoryName(dicFiles[i]), string.Concat(Path.GetFileNameWithoutExtension(dicFiles[i]), ".aff"));
				if (File.Exists(affFile) == false)
					continue; // No aff-file

				string locale = Path.GetFileNameWithoutExtension(dicFiles[i]);
				string localeDisplayName;
				if (Locales.AllLocales.TryGetValue(locale.ToLowerInvariant(), out localeDisplayName))
					_dictionaries.Add(locale, localeDisplayName);
			}

			return _dictionaries.Count > 0;
		}
	}
}
