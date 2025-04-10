﻿using System.Xml;

namespace Ginger
{
	public class TextParameter : BaseParameter<string>, IResettableParameter
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

		public override bool raw { get { return isRaw; } }

		public TextParameter() : base()
		{
		}

		public TextParameter(Recipe recipe) : base(recipe)
		{
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

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

			isRaw = xmlNode.GetAttributeBool("raw", false);
			return true;
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

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (string.IsNullOrEmpty(value))
				return;

			string sValue;
			if (isRaw)
				sValue = GingerString.FromCode(value);
			else
				sValue = GingerString.FromParameter(GingerString.EvaluateParameter(value.Trim(), state.evalContext, state.evalConfig)).ToString();

			state.SetValue(id, sValue, scope);
			state.SetValue(string.Concat(id.ToString(), ":raw"), Text.DontProcess(value.Trim()), scope);

			if (isGlobal && scope == Parameter.Scope.Global)
				state.Reserve(id, uid, sValue);
		}

		public override object Clone()
		{
			var clone = CreateClone<TextParameter>();
			clone.mode = this.mode;
			clone.isRaw = this.isRaw;
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Text".GetHashCode();
			hash ^= isRaw.GetHashCode();
			hash ^= Utility.MakeHashCode(mode);
			return hash;
		}

		public void ResetValue(string value)
		{
			this.value = value;
		}
	}

}
