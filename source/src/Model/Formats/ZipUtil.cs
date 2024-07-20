using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace Ginger
{
	public static partial class FileUtil
	{
		private static FileUtil.Error ExtractJsonFromArchive(string filename, out string tavernJsonV3, out string gingerXml)
		{
			tavernJsonV3 = null;
			gingerXml = null;
			try
			{
				using (var zip_stream = new ZipInputStream(File.OpenRead(filename)))
				{
					ZipEntry theEntry;
					while (zip_stream.CanRead && (theEntry = zip_stream.GetNextEntry()) != null)
					{
						string entryName = theEntry.Name.ToLowerInvariant();

						if (entryName == "ginger.xml")
						{
							long dataSize = theEntry.Size;
							if (dataSize > 0)
							{
								byte[] buffer = new byte[dataSize];
								zip_stream.Read(buffer, 0, (int)dataSize);
								gingerXml = new string(Encoding.UTF8.GetChars(buffer));
							}
							continue;
						}
						if (entryName == "card.json")
						{
							long dataSize = theEntry.Size;
							if (dataSize > 0)
							{
								byte[] buffer = new byte[dataSize];
								zip_stream.Read(buffer, 0, (int)dataSize);
								tavernJsonV3 = new string(Encoding.UTF8.GetChars(buffer));
							}
							continue;
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

				using (var zip_stream = new ZipInputStream(File.OpenRead(filename)))
				{
					ZipEntry theEntry;
					while (zip_stream.CanRead && (theEntry = zip_stream.GetNextEntry()) != null)
					{
						string thisEntryName = theEntry.Name.Replace('\\', '/');
						if (thisEntryName == entryName) // Exact match
						{
							long dataSize = theEntry.Size;
							if (dataSize > 0)
							{
								data = new byte[dataSize];
								zip_stream.Read(data, 0, (int)dataSize);
								return Error.NoError;
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
