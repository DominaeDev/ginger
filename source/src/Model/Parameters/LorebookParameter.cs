using System.Xml;

namespace Ginger
{
	public class LorebookParameter : BaseParameter<Lorebook>
	{
		public int pageIndex = 0; // Volatile

		public LorebookParameter() : base()
		{
		}

		public LorebookParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = true;

			value = new Lorebook();
			value.entries.Add(new Lorebook.Entry());
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("__Lorebook");
			base.SaveToXml(node);
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope) { }

		public override object Clone()
		{
			var clone = CreateClone<LorebookParameter>();
			clone.value = this.value.Clone();
			clone.pageIndex = this.pageIndex;
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Lorebook".GetHashCode();
			return hash;
		}
	}
}
