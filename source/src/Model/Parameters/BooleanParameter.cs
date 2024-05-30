using System.Xml;

namespace Ginger
{
	public class BooleanParameter : BaseParameter<bool>
	{
		public BooleanParameter() : base()
		{
		}

		public BooleanParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			defaultValue = xmlNode.GetAttributeBool("default", default(bool));
			defaultValue = xmlNode.GetValueElementBool("Default", defaultValue);
			value = defaultValue;
			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Toggle");
			base.SaveToXml(node);

			if (defaultValue)
				node.AddValueElement("Default", true);
		}

		public override void OnApplyToContext(Context context, Context localContext, ContextString.EvaluationConfig evalConfig)
		{
			if (value)
			{
				context.AddTag(id);
				localContext.AddTag(string.Concat(id.ToString(), ":local"));
			}
		}

		public override object Clone()
		{
			return CreateClone<BooleanParameter>();
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Toggle".GetHashCode();
			return hash;
		}
	}
}
