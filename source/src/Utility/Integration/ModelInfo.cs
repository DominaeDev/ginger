using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Ginger.Properties;

namespace Ginger.Integration
{
	public class BackyardModelInfo
	{
		[JsonProperty("displayName", Required = Required.Always)]
		public string displayName;
		[JsonProperty("promptFormat", Required = Required.Always)]
		public string promptTemplate;
		[JsonProperty("ctxSize", Required = Required.Always)]
		public int ctxSize;
		
		public class FileEntry
		{
			[JsonProperty("name", Required = Required.Always)]
			public string id;
			[JsonProperty("displayName", Required = Required.Always)]
			public string displayName;
			[JsonProperty("localFilename", Required = Required.Always)]
			public string filename;
			[JsonProperty("fileFormat", Required = Required.Always)]
			public string fileFormat;
		}

		[JsonProperty("files", Required = Required.Always)]
		public FileEntry[] files;
	}

	public struct BackyardModel
	{
		public string id;
		public string displayName;
		public string promptTemplate;
		public string fileFormat;

		public bool Compare(string nameOrId)
		{
			return string.Compare(nameOrId, id, StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(nameOrId, displayName, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public override string ToString()
		{
			return displayName;
		}
	}

	public static class BackyardModelDatabase
	{
		private static JsonSchema _backyardModelsSchema;

		public static IList<BackyardModel> Models { get { return _Models; } }
		private static List<BackyardModel> _Models = new List<BackyardModel>();
		private static List<BackyardModelInfo> _Entries = new List<BackyardModelInfo>();

		static BackyardModelDatabase()
		{
			_backyardModelsSchema = JsonSchema.Parse(Resources.backyard_models_schema);

			// JsonSchemaGenerator generator = new JsonSchemaGenerator();
			// JsonSchema schema = generator.Generate(typeof(BackyardModelInfo[]));
			// string jsonSchema = schema.ToString();
		}

		public static bool FindModels(string downloadPath, string json)
		{
			if (string.IsNullOrEmpty(json))
				return false;

			_Entries.Clear();
			_Models.Clear();

			string[] modelFiles;
			try
			{
				modelFiles = Directory.EnumerateFiles(downloadPath, "*.gguf")
					.Union(Directory.EnumerateFiles(downloadPath, "*.bin"))
						.ToArray();

				if (modelFiles.Length == 0)
					return false; // No models found
			}
			catch
			{
				return false; // No models found
			}

			try
			{
				JArray list = JArray.Parse(json);
				if (list.IsValid(_backyardModelsSchema))
				{
					foreach (var entry in list)
					{
						BackyardModelInfo modelInfo = entry.ToObject<BackyardModelInfo>();
						_Entries.Add(modelInfo);
					}
				}
			}
			catch
			{
			}

			_Models = modelFiles
				.Select(fn => Path.GetFileName(fn))
				.Select(fn => {
					var modelInfo = _Entries.FirstOrDefault(e => e.files.ContainsAny(f => string.Compare(f.filename, fn, StringComparison.OrdinalIgnoreCase) == 0));
					if (modelInfo != null)
					{
						var fileInfo = modelInfo.files.FirstOrDefault(f => string.Compare(f.filename, fn, StringComparison.OrdinalIgnoreCase) == 0);
						return new BackyardModel() {
							id = fileInfo.id,
							displayName = fileInfo.displayName,
							promptTemplate = modelInfo.promptTemplate,
							fileFormat = Utility.GetFileExt(fn),
						};
					}
					else
					{
						return new BackyardModel() {
							id = fn,
							displayName = Path.GetFileNameWithoutExtension(fn),
							fileFormat = Utility.GetFileExt(fn),
							promptTemplate = null,
						};
					}
				})
				.OrderBy(i => i.displayName)
				.ToList();
			return true;
		}

		public static BackyardModel GetModel(string modelId)
		{
			return _Models.FirstOrDefault(m => m.Compare(modelId));
		}
	}
}
