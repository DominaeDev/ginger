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
			[JsonProperty("cloudPlan", Required = Required.Default, NullValueHandling = NullValueHandling.Include)]
			public string cloudPlan;
			[JsonProperty("isDeprecated", Required = Required.Default)]
			public bool isDeprecated;
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
		public bool isCustomLocalModel;

		public enum CloudPlan
		{
			Undefined = 0,
			Free,
			Standard,
			Advanced,
			Pro,
		}
		public CloudPlan cloudPlan;
		public bool isCloudModel { get { return cloudPlan != CloudPlan.Undefined; } }

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

		private static string FreeCloudModel = "cloud.llama2.7b.fimbulvetr.gguf_v2.q8_0";

		static BackyardModelDatabase()
		{
			_backyardModelsSchema = JsonSchema.Parse(Resources.backyard_models_schema);

//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(BackyardModelInfo[]));
//			string jsonSchema = schema.ToString();
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
				IList<string> errors;
				if (list.IsValid(_backyardModelsSchema, out errors))
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
							isCustomLocalModel = false,
							cloudPlan = BackyardModel.CloudPlan.Undefined,
						};
					}
					else
					{
						return new BackyardModel() {
							id = fn,
							displayName = Path.GetFileNameWithoutExtension(fn),
							fileFormat = Utility.GetFileExt(fn),
							promptTemplate = null,
							isCustomLocalModel = true,
							cloudPlan = BackyardModel.CloudPlan.Undefined,
						};
					}
				})
				.OrderBy(i => i.displayName)
				.ToList();

			// Cloud models
			_Models.AddRange(_Entries
				.Where(m => m.files.ContainsAny(f => f.cloudPlan != null))
				.SelectMany(m => 
				{
					return m.files
						.Where(f => f.isDeprecated == false && f.cloudPlan != null)
						.Select(f => {
							BackyardModel.CloudPlan cloudPlan = EnumHelper.FromString(f.cloudPlan, BackyardModel.CloudPlan.Undefined);
							if (string.Compare(f.id, FreeCloudModel, StringComparison.Ordinal) == 0)
								cloudPlan = BackyardModel.CloudPlan.Free;

							return new BackyardModel() {
								id = f.id,
								displayName = string.Format("Cloud ({0}) - {1}", EnumHelper.ToString(cloudPlan), m.displayName),
								promptTemplate = m.promptTemplate,
								fileFormat = "",
								isCustomLocalModel = false,
								cloudPlan = cloudPlan,
							};
						})
						.Where(mm => mm.cloudPlan != BackyardModel.CloudPlan.Undefined);
				})
				.OrderBy(mm => mm.cloudPlan)
				.ThenBy(mm => mm.displayName)
			);
			return true;
		}

		public static BackyardModel GetModel(string modelId)
		{
			return _Models.FirstOrDefault(m => m.Compare(modelId));
		}
	}
}
