using System;

namespace Ginger
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class IniConfigSectionAttribute : System.Attribute
	{
		public string name { get { return _name; } }
		private string _name;

		public IniConfigSectionAttribute()
		{
		}

		public IniConfigSectionAttribute(string name)
		{
			_name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public class IniConfigValueAttribute : System.Attribute
	{
		public string name { get { return _name; } }
		private string _name;

		public bool bEnforceDataLength { get { return _bEnforceDataLength; } }
		private bool _bEnforceDataLength = false;

		public IniConfigValueAttribute()
		{
			_name = null;
		}

		public IniConfigValueAttribute(string name, bool bEnforceDataLength = false)
		{
			_name = name;
			_bEnforceDataLength = bEnforceDataLength;
		}
	}

}
