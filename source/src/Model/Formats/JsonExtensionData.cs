using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public class JsonExtensionData : ICloneable
	{
		[JsonExtensionData]
		private Dictionary<string, JToken> _additionalData;

		public virtual GingerJsonExtensionData WithGinger() // Character
		{
			return new GingerJsonExtensionData() {
				_additionalData = _additionalData != null ? new Dictionary<string, JToken>(this._additionalData) : null,
			};
		}

		public virtual SmallGingerJsonExtensionData WithGingerVersion() // Lorebook
		{
			return new SmallGingerJsonExtensionData() {
				_additionalData = _additionalData != null ? new Dictionary<string, JToken>(this._additionalData) : null,
			};
		}

		public object Clone()
		{
			return new JsonExtensionData() {
				_additionalData = _additionalData
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone())
			};
		}
	}

	public class GingerJsonExtensionData : JsonExtensionData
	{
		[JsonProperty("ginger")]
		public GingerExtensionData ginger = new GingerExtensionData();

		public override GingerJsonExtensionData WithGinger()
		{
			return this;
		}
	}

	public class SmallGingerJsonExtensionData : JsonExtensionData
	{
		[JsonProperty("ginger")]
		public GingerVersionExtensionData ginger = new GingerVersionExtensionData();
	}
}
