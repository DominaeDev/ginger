using System.Xml;

namespace Ginger
{
	public class TextParameter : BaseParameter<string>
	{
		public enum Mode
		{
			Single,
			Short,
			Brief,
			Code,
			Chat,
			Flexible,
			Component,
		}
		public Mode mode = Mode.Single;
		public bool isRaw = false;

		public TextParameter() : base()
		{
		}

		public TextParameter(Recipe recipe) : base(recipe)
		{
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			string style = xmlNode.GetAttribute("style", "").ToLowerInvariant();
			switch (style)
			{
			case "short":
				mode = Mode.Short;
				break;
			case "multi":
				mode = Mode.Brief;
				break;
			case "flex":
				mode = Mode.Flexible;
				break;
			default:
				mode = EnumHelper.FromString(style, Mode.Single);
				break;
			}

			defaultValue = xmlNode.GetAttribute("default", default(string));
			defaultValue = xmlNode.GetValueElement("Default", defaultValue);
			value = defaultValue;
			isRaw = xmlNode.GetAttributeBool("raw", false);

			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Text");
			base.SaveToXml(node);

			node.AddAttribute("style", EnumHelper.ToString(mode).ToLowerInvariant());

			if (string.IsNullOrEmpty(defaultValue) == false)
				node.AddValueElement("Default", defaultValue);

			if (isRaw)
				node.AddAttribute("raw", true);
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (string.IsNullOrEmpty(value) == false)
			{
				string sValue;
				if (isRaw)
				{
					if (mode == Mode.Code)
						sValue = GingerString.FromCode(value); // Keep tabs
					else
						sValue = Text.DontProcess(value.Trim());
				}
				else
					sValue = GingerString.FromParameter(GingerString.EvaluateParameter(value.Trim(), state.evalContext, state.evalConfig)).ToString();

				state.SetValue(id, sValue, scope);
				state.SetValue(string.Concat(id.ToString(), ":raw"), Text.DontProcess(value.Trim()), scope);
			}
		}

		public override object Clone()
		{
			var clone = CreateClone<TextParameter>();
			clone.mode = this.mode;
			clone.isRaw = this.isRaw;
			return clone;
		}

		public override void ResetToDefault() // Called on instantiation
		{
			Context context = Current.Character.GetContext(CharacterData.ContextType.WithFlags);

			// Evaluate default value
			if (string.IsNullOrEmpty(defaultValue) == false)
			{
				value = GingerString.FromString(Text.Eval(defaultValue, context,
					new ContextString.EvaluationConfig() {
						macroSuppliers = new IMacroSupplier[] { Current.Strings },
						referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
						ruleSuppliers = new IRuleSupplier[] { Current.Strings },
					},
					Text.EvalOption.Minimal)).ToBaked();
				
				return;
			}

			base.ResetToDefault();			
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Text".GetHashCode();
			hash ^= isRaw.GetHashCode();
			hash ^= Utility.MakeHashCode(mode);
			return hash;
		}

	}

}
