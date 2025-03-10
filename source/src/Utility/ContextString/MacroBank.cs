using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public interface IMacroSupplier
	{
		MacroBank GetMacroBank();
	}

	public class MacroBank : IMacroSupplier
	{
		public IEnumerable<KeyValuePair<StringHandle, string>> macros { get { return _macros; } }
		private Dictionary<StringHandle, string> _macros = new Dictionary<StringHandle, string>();
		public IEnumerable<KeyValuePair<StringHandle, string>> wrappers { get { return _wrappers; } }
		private Dictionary<StringHandle, string> _wrappers = new Dictionary<StringHandle, string>();

		public MacroBank GetMacroBank()
		{
			return this;
		}

		public bool LoadFromXml(XmlNode macroNode)
		{
			var ids = Utility.ListFromCommaSeparatedString(macroNode.GetAttribute("id")).Select(s => new StringHandle(s));
			string value = macroNode.GetTextValue();

			// Clean up line-breaks from macros
			CleanMacro(ref value);

			foreach (var id in ids)
				_macros.Set(id, value);
			
			return true;			
		}

		public bool LoadWrapperFromXml(XmlNode wrapperNode)
		{
			string id = wrapperNode.GetAttribute("id");
			var ids = Utility.ListFromCommaSeparatedString(id).Select(s => new StringHandle(s));
			string value = wrapperNode.GetTextValue();

			if (value.IndexOf('_') != -1) // Must contain character '_'
			{
				foreach (var wrapperID in ids)
					_wrappers.Set(wrapperID, value);
			}

			return true;			
		}

		private void CleanMacro(ref string value)
		{
			var bad_chars = new char[] { '\r', '\n', '\t' };

			value = value.Trim();

			int pos = value.IndexOfAny(bad_chars, 0);
			if (pos == -1)
				return;

			var sb = new StringBuilder(value);
			while (pos != -1)
			{
				switch(sb[pos])
				{
				case '\n':
				case '\t':
					sb[pos++] = ' ';
					break;
				default:
					sb.Remove(pos, 1);
					break;
				}
				pos = sb.IndexOfAny(bad_chars, pos);
			}

			value = sb.ToString();
		}

		public int Count()
		{
			return _macros.Count;
		}

		public void AddMacro(StringHandle id, string value)
		{
			if (StringHandle.IsNullOrEmpty(id) == false)
				_macros.Add(id, string.IsNullOrEmpty(value) ? "" : value.Trim());
		}

		public bool HasMacro(StringHandle id)
		{
			return _macros.ContainsKey(id);
		}

		public bool TryGetMacro(StringHandle id, out string value)
		{
			// User macro?
			if (_macros.TryGetValue(id, out value))
				return true;

			value = default(string);
			return false;
		}

		public bool HasWrapper(StringHandle id)
		{
			return _wrappers.ContainsKey(id);
		}

		public bool TryGetWrapper(StringHandle id, out string value)
		{
			// User macro?
			if (_wrappers.TryGetValue(id, out value))
				return true;

			value = default(string);
			return false;
		}
		
		public void Clear()
		{
			_macros.Clear();
			_wrappers.Clear();
		}

		public void AppendMacros(MacroBank other)
		{
			foreach (var kvp in other._macros)
				AddMacro(kvp.Key, kvp.Value);
		}

		public override int GetHashCode()
		{
			int hash = 0x3C34CECB;
			foreach (var kvp in _macros)
				hash ^= Utility.MakeHashCode(Utility.HashOption.Ordered, kvp.Key, kvp.Value);
			foreach (var kvp in _wrappers)
				hash ^= Utility.MakeHashCode(Utility.HashOption.Ordered, kvp.Key, kvp.Value);
			return hash;
		}
	}
}
