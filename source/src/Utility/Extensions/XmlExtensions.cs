using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Ginger
{
	public interface IXmlSaveable
	{
		void SaveToXml(XmlNode xmlNode);
	}

	public interface IXmlLoadable
	{
		bool LoadFromXml(XmlNode xmlNode);
	}

	public static class XmlLoadableExtensions
	{
		public static bool LoadFromXml(this IXmlLoadable instance, string filename, string rootElement = null)
		{
			byte[] buffer = Utility.LoadFile(filename);
			if (buffer == null || buffer.Length == 0)
				return false;

			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				using (var stream = new MemoryStream(buffer))
				{
					xmlDoc.Load(stream);
					if (rootElement != null && string.Compare(xmlDoc.DocumentElement.Name, rootElement) != 0)
						return false;
					return instance.LoadFromXml(xmlDoc.DocumentElement);
				}
			}
			catch
			{
				return false;
			}
		}

	}

	public static class XmlNodeExtensions
	{
		public static XmlElement AddElement(this XmlNode node, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			XmlDocument xmlDoc = node.OwnerDocument;
			try
			{
				var element = xmlDoc.CreateElement(name);
				node.AppendChild(element);
				return element;
			}
			catch
			{
				return null;
			}
		}

		public static XmlNode AddValueElement(this XmlNode node, string name, string value)
		{
			XmlElement xmlNode = AddElement(node, name);
			if (xmlNode == null)
				return null;

			XmlDocument xmlDoc = node.OwnerDocument;
			XmlNode valueNode = xmlDoc.CreateTextNode(value);
			xmlNode.AppendChild(valueNode);
			return xmlNode;
		}

		public static XmlNode AddValueElement(this XmlNode node, string name, int value)
		{
			return AddValueElement(node, name, value.ToString());
		}

		public static XmlNode AddValueElement(this XmlNode node, string name, float value)
		{
			return AddValueElement(node, name, value.ToString("0.000", CultureInfo.InvariantCulture));
		}
		
		public static XmlNode AddValueElement(this XmlNode node, string name, double value)
		{
			return AddValueElement(node, name, value.ToString("0.000", CultureInfo.InvariantCulture));
		}

		public static XmlNode AddValueElement(this XmlNode node, string name, bool value)
		{
			return AddValueElement(node, name, value ? "True" : "False");
		}

		public static XmlNode AddValueElement(this XmlNode node, string name, decimal value)
		{
			return AddValueElement(node, name, value.ToString(CultureInfo.InvariantCulture));
		}

		public static XmlNode AddValueElementEnum<T>(this XmlNode node, string name, T value) where T : struct, IConvertible
		{
			return AddValueElement(node, name, EnumHelper.ToString(value));
		}

		public static XmlNode AddValueElement<T>(this XmlNode node, string name, IRange<T> value)
		{
			return AddValueElement(node, name, value.ToString());
		}

		public static bool RemoveTextValue(this XmlNode node)
		{
			var child = node.FirstChild;
			if ((child != null) && (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA))
			{
				node.RemoveChild(child);
				return true;
			}
			return false;
		}

		public static void AddTextValue(this XmlNode node, string value)
		{
			XmlDocument xmlDoc = node.OwnerDocument;
			XmlNode valueNode = xmlDoc.CreateTextNode(value);
			node.AppendChild(valueNode);
		}

		public static void AddTextValue(this XmlNode node, int value)
		{
			AddTextValue(node, value.ToString());
		}

		public static void AddTextValue(this XmlNode node, float value)
		{
			AddTextValue(node, value.ToString("0.000", CultureInfo.InvariantCulture));
		}
		
		public static void AddTextValue(this XmlNode node, double value)
		{
			AddTextValue(node, value.ToString("0.000", CultureInfo.InvariantCulture));
		}

		public static void AddTextValue(this XmlNode node, decimal value)
		{
			AddTextValue(node, value.ToString(CultureInfo.InvariantCulture));
		}

		public static void AddTextValue<T>(this XmlNode node, IRange<T> value)
		{
			AddTextValue(node, value.ToString());
		}

		public static bool AddAttribute(this XmlNode node, string name, string value)
		{
			if (!(node is XmlElement))
				return false;

			(node as XmlElement).SetAttribute(name, value);
			return true;
		}

		public static bool AddAttribute(this XmlNode node, string name, int value)
		{
			return AddAttribute(node, name, value.ToString());
		}

		public static bool AddAttribute(this XmlNode node, string name, long value)
		{
			return AddAttribute(node, name, value.ToString());
		}

		public static bool AddAttribute(this XmlNode node, string name, float value)
		{
			return AddAttribute(node, name, value.ToString("0.000", CultureInfo.InvariantCulture));
		}

		public static bool AddAttribute(this XmlNode node, string name, double value)
		{
			return AddAttribute(node, name, value.ToString("0.000", CultureInfo.InvariantCulture));
		}

		public static bool AddAttribute(this XmlNode node, string name, decimal value)
		{
			return AddAttribute(node, name, value.ToString(CultureInfo.InvariantCulture));
		}

		public static bool AddAttribute(this XmlNode node, string name, bool value)
		{
			return AddAttribute(node, name, value ? "true" : "false");
		}
		
		public static bool AddAttribute<T>(this XmlNode node, string name, IRange<T> value)
		{
			return AddAttribute(node, name, value.ToString());
		}

		public static bool HasAttribute(this XmlNode node, string name)
		{
			if (node == null || !(node is XmlElement))
				return false;
			return node.Attributes.GetNamedItem(name) != null;
		}

		public static string GetAttribute(this XmlNode node, string name, string default_value = "")
		{
			if (!(node is XmlElement))
				return default_value;

			var attribute = (node as XmlElement).GetAttributeNode(name);
			if (attribute == null)
				return default_value;

			return attribute.Value;
		}

		public static int GetAttributeInt(this XmlNode node, string name, int default_value = 0)
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			int outValue;
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue;
			return default_value;
		}

		public static uint GetAttributeUint(this XmlNode node, string name, uint default_value = 0)
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			uint outValue;
			if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue;
			return default_value;
		}

		public static long GetAttributeLong(this XmlNode node, string name, long default_value = 0)
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			long outValue;
			if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue;
			return default_value;
		}

		public static float GetAttributeFloat(this XmlNode node, string name, float default_value = 0.0f)
		{
			return (float)GetAttributeDouble(node, name, default_value);
		}

		public static decimal GetAttributeDecimal(this XmlNode node, string name, decimal default_value = 0.0m)
		{
			var value = GetAttributeDouble(node, name, double.NaN);
			if (double.IsNaN(value))
				return default_value;
			return Convert.ToDecimal(value);
		}

		public static double GetAttributeDouble(this XmlNode node, string name, double default_value = 0.0)
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			double outValue;
			if (value.EndsWith("%"))
			{
				if (double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
					return (float)(outValue / 100.0);
				return default_value;
			}
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
				return outValue;
			return default_value;
		}

		public static bool GetAttributeBool(this XmlNode node, string name, bool default_value = false)
		{
			string strValue = GetAttribute(node, name, null);
			if (strValue == null)
				return default_value;

			int outValue;
			if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue != 0;

			return string.Compare(strValue, "true", true) == 0 
				|| string.Compare(strValue, "yes", true) == 0
				|| string.Compare(strValue, "on", true) == 0;
		}
		
		public static bool GetAttributeEnum<T>(this XmlNode node, string name, out T eValue) where T : struct, IConvertible
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
			{
				eValue = default(T);
				return false;
			}

			return EnumInfo<T>.Convert(value, out eValue);
		}

		public static T GetAttributeEnum<T>(this XmlNode node, string name, T default_value = default(T)) where T : struct, IConvertible
		{
			T outValue;
			if (GetAttributeEnum(node, name, out outValue))
				return outValue;
			return default_value;
		}

		public static bool GetAttributeEnumInt<T>(this XmlNode node, string name, out T eValue) where T : struct, IConvertible
		{
			int value = GetAttributeInt(node, name, int.MinValue);
			if (value == int.MinValue)
			{
				eValue = default(T);
				return false;
			}
			return EnumInfo<T>.Convert(value, out eValue);
		}

		public static T GetAttributeEnumInt<T>(this XmlNode node, string name, T default_value = default(T)) where T : struct, IConvertible
		{
			T outValue;
			if (GetAttributeEnumInt(node, name, out outValue))
				return outValue;
			return default_value;
		}

		public static Range GetAttributeRange(this XmlNode node, string name, Range default_value = default(Range))
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			Range range;
			if (Range.TryParse(value, out range))
				return range;
			return default_value;
		}

		public static RangeInt GetAttributeRangeInt(this XmlNode node, string name, RangeInt default_value = default(RangeInt))
		{
			string value = GetAttribute(node, name, null);
			if (value == null)
				return default_value;

			RangeInt range;
			if (RangeInt.TryParse(value, out range))
				return range;
			return default_value;
		}
		
		public static XmlNode GetFirstElement(this XmlNode node, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			var child = node.FirstChild;
			while (child != null)
			{
				if (child.Name == name)
					return child;
				child = child.NextSibling;
			}

			return null;
		}

		public static XmlNode GetNextSibling(this XmlNode node)
		{
			string name = node.Name;
			var sibling = node.NextSibling;
			while (sibling != null)
			{
				if (sibling.Name == name)
					return sibling;
				sibling = sibling.NextSibling;
			}

			return null;
		}

		public static XmlNode GetFirstElementAny(this XmlNode node)
		{
			var child = node.FirstChild;
			while (child != null)
			{
				if (child.NodeType == XmlNodeType.Element)
					return child;
				child = child.NextSibling;
			}

			return null;
		}

		public static XmlNode GetNextSiblingAny(this XmlNode node)
		{
			var sibling = node.NextSibling;
			while (sibling != null)
			{
				if (sibling.NodeType == XmlNodeType.Element)
					return sibling;
				sibling = sibling.NextSibling;
			}

			return null;
		}

		public static string GetValueElement(this XmlNode node, string name, string default_value = "")
		{
			if (string.IsNullOrEmpty(name))
				return default_value;

			var valueNode = GetFirstElement(node, name);
			if (valueNode == null)
				return default_value;

			var child = valueNode.FirstChild;
			while (child != null)
			{
				if (child.NodeType == XmlNodeType.Text)
					return Utility.Unindent(CapLength(child.Value).TrimStart());
				else if (child.NodeType == XmlNodeType.CDATA)
					return CapLength(child.Value);
				child = child.NextSibling;
			}

			return default_value;
		}

		public static string GetValueElementNoLinebreak(this XmlNode node, string name, string default_value = "")
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			StringBuilder sb = new StringBuilder(value);
			sb.Replace("\r\n", " ");
			sb.Replace("\n", " ");
			sb.Replace("\r", " ");
			sb.Replace("\t", " ");
			sb.Replace("  ", " ");
			return sb.ToString();
		}

		public static int GetValueElementInt(this XmlNode node, string name, int default_value = 0)
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			int outValue;
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue;
			return default_value;
		}

		public static float GetValueElementFloat(this XmlNode node, string name, float default_value = 0.0f)
		{
			return (float)GetValueElementDouble(node, name, default_value);
		}

		public static decimal GetValueElementDecimal(this XmlNode node, string name, decimal default_value = 0.0m)
		{
			double value = GetValueElementDouble(node, name, double.NaN);
			if (double.IsNaN(value))
				return default_value;
			return Convert.ToDecimal(value);
		}

		public static double GetValueElementDouble(this XmlNode node, string name, double default_value = 0.0)
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			double outValue;
			if (value.EndsWith("%"))
			{
				if (double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
					return (float)(outValue / 100.0);
				return default_value;
			}
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
				return outValue;
			return default_value;
		}

		public static bool GetValueElementBool(this XmlNode node, string name, bool default_value = false)
		{
			string strValue = GetValueElement(node, name, null);
			if (strValue == null)
				return default_value;

			int outValue;
			if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue != 0;

			return string.Compare(strValue, "true", true) == 0
				|| string.Compare(strValue, "yes", true) == 0
				|| string.Compare(strValue, "on", true) == 0;
		}

		public static bool GetValueElementEnum<T>(this XmlNode node, string name, out T eValue) where T : struct, IConvertible
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
			{
				eValue = default(T);
				return false;
			}

			return EnumInfo<T>.Convert(value, out eValue);
		}

		public static T GetValueElementEnum<T>(this XmlNode node, string name, T default_value = default(T)) where T : struct, IConvertible
		{
			T outValue;
			if (GetValueElementEnum(node, name, out outValue))
				return outValue;
			return default_value;
		}

		public static bool GetValueElementEnumInt<T>(this XmlNode node, string name, out T eValue) where T : struct, IConvertible
		{
			int value = GetValueElementInt(node, name, 0);
			return EnumInfo<T>.Convert(value, out eValue);
		}

		public static T GetValueElementEnumInt<T>(this XmlNode node, string name, T default_value = default(T)) where T : struct, IConvertible
		{
			T outValue;
			if (GetValueElementEnumInt(node, name, out outValue))
				return outValue;
			return default_value;
		}

		public static Range GetValueElementRange(this XmlNode node, string name, Range default_value = default(Range))
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			Range range;
			if (Range.TryParse(value, out range))
				return range;
			return default_value;
		}

		public static RangeInt GetValueElementRangeInt(this XmlNode node, string name, RangeInt default_value = default(RangeInt))
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			RangeInt range;
			if (RangeInt.TryParse(value, out range))
				return range;
			return default_value;
		}

		public static RangeEnum<T> GetValueElementRangeEnum<T>(this XmlNode node, string name, RangeEnum<T> default_value = default(RangeEnum<T>)) where T : struct, System.IConvertible
		{
			string value = GetValueElement(node, name, null);
			if (value == null)
				return default_value;

			int hyphen = value.IndexOf(Range.Delimiter);
			if (hyphen == -1)
			{
				T enumValue = EnumHelper.FromString<T>(value.Trim());
				return new RangeEnum<T>(enumValue, enumValue);
			}

			T min = EnumHelper.FromString<T>(value.Substring(0, hyphen).Trim());
			T max = EnumHelper.FromString<T>(value.Substring(hyphen+1).Trim());
			return new RangeEnum<T>(min, max);
		}

		public static bool HasTextValue(this XmlNode node)
		{
			var child = node.FirstChild;
			return (child != null) && (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA);
		}

		public static bool HasChildren(this XmlNode node)
		{
			var child = node.FirstChild;
			while (child != null && child.NodeType == XmlNodeType.Comment) { child = child.NextSibling; } // Skip comments
			return (child != null) && (child.NodeType == XmlNodeType.Element);
		}

		public static string GetTextValue(this XmlNode node, string default_value = "")
		{
			StringBuilder sbValue = new StringBuilder();

			var child = node.FirstChild;
			while (child != null)
			{
				if (child.NodeType == XmlNodeType.Text)
					sbValue.Append(Utility.Unindent(CapLength(child.Value).TrimStart()));
				else if (child.NodeType == XmlNodeType.CDATA)
					sbValue.Append(CapLength(child.Value));
				child = child.NextSibling;
			}

			if (sbValue.Length > 0)
				return sbValue.ToString();
			return default_value;
		}

		public static int GetTextValueInt(this XmlNode node, int default_value = 0)
		{
			string value = GetTextValue(node, null);
			if (value == null)
				return default_value;

			int outValue;
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue;
			return default_value;
		}

		public static float GetTextValueFloat(this XmlNode node, float default_value = 0.0f)
		{
			return (float)GetTextValueDouble(node, default_value);
		}

		public static double GetTextValueDouble(this XmlNode node, double default_value = 0.0f)
		{
			string value = GetTextValue(node, null);
			if (value == null)
				return default_value;

			double outValue;
			if (value.EndsWith("%"))
			{
				if (double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
					return (float)(outValue / 100.0);
				return default_value;
			}
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
				return outValue;
			return default_value;
		}

		public static decimal GetTextValueDecimal(this XmlNode node, decimal default_value = 0.0m)
		{
			double value = GetTextValueDouble(node, double.NaN);
			if (double.IsNaN(value))
				return default_value;
			return Convert.ToDecimal(value);
		}

		public static bool GetTextValueBool(this XmlNode node, bool default_value = false)
		{
			string strValue = GetTextValue(node, null);
			if (strValue == null)
				return default_value;

			int outValue;
			if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue))
				return outValue != 0;

			return string.Compare(strValue, "true", true) == 0
				|| string.Compare(strValue, "yes", true) == 0
				|| string.Compare(strValue, "on", true) == 0;
		}
		
		public static bool GetTextValueEnum<T>(this XmlNode node, out T eValue) where T : struct, IConvertible
		{
			string value = GetTextValue(node, null);
			if (value == null)
			{
				eValue = default(T);
				return false;
			}

			return EnumInfo<T>.Convert(value, out eValue);
		}

		public static T GetTextValueEnum<T>(this XmlNode node, T default_value = default(T)) where T : struct, IConvertible
		{
			T outValue;
			if (GetTextValueEnum(node, out outValue))
				return outValue;
			return default_value;
		}

		public static Range GetTextValueRange(this XmlNode node, Range default_value = default(Range))
		{
			string value = GetTextValue(node, null);
			if (value == null)
				return default_value;

			Range range;
			if (Range.TryParse(value, out range))
				return range;
			return default_value;
		}

		public static RangeInt GetTextValueRangeInt(this XmlNode node, RangeInt default_value = default(RangeInt))
		{
			string value = GetTextValue(node, null);
			if (value == null)
				return default_value;

			RangeInt range;
			if (RangeInt.TryParse(value, out range))
				return range;
			return default_value;
		}

		public static int GetElementCount(this XmlNode node, string name)
		{
			int count = 0;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.NodeType == XmlNodeType.Element && child.Name == name)
					++count;
			}
			return count;
		}

		public static void AddCData(this XmlNode node, string data)
		{
			XmlCDataSection cdata = node.OwnerDocument.CreateCDataSection(data);
			node.AppendChild(cdata);
		}
		
		public static void AddCData(this XmlNode node, byte[] data)
		{
			XmlCDataSection cdata = node.OwnerDocument.CreateCDataSection(Convert.ToBase64String(data));
			node.AppendChild(cdata);
		}

		public static byte[] GetCData(this XmlNode node)
		{
			string data = GetTextValue(node);
			return Convert.FromBase64String(data);
		}

		private static string CapLength(string s, uint maxLength = 32768)
		{
			if (s == null || s.Length <= maxLength)
				return s;
			return s.Substring(0, (int)maxLength);
		}

	}
}
