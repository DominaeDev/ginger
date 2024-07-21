using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static partial class FileUtil
	{
		private static Error ExtractJsonFromArchive(string filename, out string tavernJsonV3, out string gingerXml)
		{
			tavernJsonV3 = null;
			gingerXml = null;
			try
			{
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							string entryName = entry.FullName.ToLowerInvariant();

							if (entryName == "ginger.xml")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									gingerXml = new string(Encoding.UTF8.GetChars(buffer));
								}
								continue;
							}
							if (entryName == "card.json")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									tavernJsonV3 = new string(Encoding.UTF8.GetChars(buffer));
								}
								continue;
							}
						}
					}
				}

				if (string.IsNullOrEmpty(tavernJsonV3) && string.IsNullOrEmpty(gingerXml))
					return Error.NoDataFound;
				return Error.NoError;
			}
			catch
			{
				return Error.FileReadError;
			}
		}

		private static Error ExtractFileFromArchive(string filename, string entryName, out byte[] data)
		{
			try
			{
				entryName = entryName.Replace('\\', '/');

				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							string thisEntryName = entry.FullName.Replace('\\', '/');
							if (thisEntryName == entryName) // Exact match
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									data = new byte[dataSize];
									dataStream.Read(data, 0, (int)dataSize);
									return Error.NoError;
								}
							}
						}
					}
				}

				data = null;
				return Error.NoDataFound;
			}
			catch
			{
				data = null;
				return Error.FileReadError;
			}
		}

		private static Image LoadPortaitImageFromArchive(string filename, TavernCardV3 cardData)
		{
			if (cardData == null || cardData.data == null || cardData.data.assets == null || cardData.data.assets.Length == 0)
				return null;

			var iconAsset = cardData.data.assets.FirstOrDefault(a => a.type == "icon");
			if (iconAsset == null)
				return null;

			string uri = iconAsset.uri.Trim();

			if (string.IsNullOrEmpty(uri) || string.Compare("uri", "ccdefault:", StringComparison.OrdinalIgnoreCase) == 0)
				return null;

			int idxProtocol = uri.IndexOf("://");
			if (idxProtocol == -1)
				return null;

			string protocol = uri.Substring(0, idxProtocol).ToLowerInvariant();
			string path = uri.Substring(idxProtocol + 3);
			if (protocol != "embeded" && protocol != "embedded")
				return null; // Only embedded assets are supported

			byte[] data;
			var error = ExtractFileFromArchive(filename, path, out data);
			if (error != Error.NoError)
				return null; // Invalid path

			try
			{
				using (var stream = new MemoryStream(data))
				{
					return Image.FromStream(stream);
				}
			}
			catch
			{
				return null;
			}
		}
	}
}
