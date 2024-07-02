using System.Collections.Generic;

namespace Ginger
{
	public static class BlockStyles
	{
		private static Dictionary<StringHandle, Block.Style> _styles = new Dictionary<StringHandle, Block.Style>();

		public static void LoadStyles()
		{
			_styles.Clear();
			var xmlDoc = Utility.LoadXmlDocument(Utility.ContentPath("Internal", "node_styles.xml"));
			if (xmlDoc == null)
				return;

			var xmlRoot = xmlDoc.DocumentElement;
			if (xmlRoot.Name != "Ginger")
				return;

			var styleNode = xmlRoot.GetFirstElement("Style");
			while (styleNode != null)
			{
				StringHandle id = styleNode.GetAttribute("id", null);
				string styleName = styleNode.GetTextValue();
				Block.Style style = FromString(styleName);

				if (StringHandle.IsNullOrEmpty(id) == false && style != Block.Style.Undefined)
					_styles.Set(id, style);

				styleNode = styleNode.GetNextSibling();
			}

		}

		public static Block.Style FromString(string styleName)
		{
			if (string.IsNullOrEmpty(styleName))
				return Block.Style.Undefined;

			switch (styleName)
			{
			case "default":
				return Block.Style.Default;
			case "comma-list":
				return Block.Style.CommaList;
			case "comma-stop":
				return Block.Style.CommaStop;
			case "plus-list":
				return Block.Style.PlusList;
			case "plus-array":
				return Block.Style.PlusArray;
			case "plus-group":
				return Block.Style.PlusGroup;
			case "semi-group":
				return Block.Style.SemiGroup;
			case "number-list":
				return Block.Style.Number;
			case "bullet-list":
				return Block.Style.Bullet;
			case "parenthesis":
			case "parentheses":
				return Block.Style.Parenthesis;
			case "bracket":
			case "brackets":
				return Block.Style.Bracket;
			}

			Block.Style style;
			if (_styles.TryGetValue(new StringHandle(styleName), out style))
				return style;
			return EnumHelper.FromString(styleName, Block.Style.Undefined);
		}

		public static string ToString(Block.Style mode)
		{
			switch (mode)
			{
			case Block.Style.CommaList:
				return "comma-list";
			case Block.Style.CommaStop:
				return "comma-stop";
			case Block.Style.PlusList:
				return "plus-list";
			case Block.Style.PlusArray:
				return "plus-array";
			case Block.Style.PlusGroup:
				return "plus-group";
			case Block.Style.SemiGroup:
				return "semi-group";
			case Block.Style.Number:
				return "number-list";
			case Block.Style.Bullet:
				return "bullet-list";
			case Block.Style.Undefined:
				return "";
			default:
				return EnumHelper.ToString(mode).ToLowerInvariant();
			}
		}

		public static bool IsHorizontalList(Block.Style style)
		{
			return style >= Block.Style.Comma && style < Block.Style.Line;
		}

		public static bool IsVerticalList(Block.Style style)
		{
			return style >= Block.Style.Number && style <= Block.Style.Bullet;
		}
	}
}
