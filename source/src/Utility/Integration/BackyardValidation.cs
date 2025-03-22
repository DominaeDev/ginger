using System.Collections.Generic;

namespace Ginger.Integration
{
	public enum BackyardDatabaseVersion
	{
		Unknown,
		Version_0_28_0,		// Groups (db)
		Version_0_29_0,		// Chat backgrounds (Canary 0.28.27)
		Version_0_37_0,		// Parties
	}

	public static class VersionConstants
	{
		public static readonly VersionNumber Version_0_28_0 = new VersionNumber(0, 28, 0);
		public static readonly VersionNumber Version_0_29_0 = new VersionNumber(0, 29, 0);
		public static readonly VersionNumber Version_0_37_0 = new VersionNumber(0, 37, 0);
	}

	public static class BackyardValidation
	{
		public static BackyardDatabaseVersion DatabaseVersion = BackyardDatabaseVersion.Unknown;

		public enum Feature
		{
			Undefined = 0,
			ChatBackgrounds,
			PartyChats,
			PartyNames,
		}

		public static bool CheckFeature(Feature feature)
		{
			if (DatabaseVersion == BackyardDatabaseVersion.Unknown)
				return false;

			switch (feature)
			{
			case Feature.ChatBackgrounds:
				return DatabaseVersion >= BackyardDatabaseVersion.Version_0_29_0;
			case Feature.PartyChats:
			case Feature.PartyNames:
				return DatabaseVersion >= BackyardDatabaseVersion.Version_0_37_0;
			}
			return false;
		}

		public static Dictionary<BackyardDatabaseVersion, string[][]> TablesByVersion = new Dictionary<BackyardDatabaseVersion, string[][]> 
		{
			{ BackyardDatabaseVersion.Version_0_28_0, BackyardDatabaseTables.Version28 },
			{ BackyardDatabaseVersion.Version_0_29_0, BackyardDatabaseTables.Version29 },
			{ BackyardDatabaseVersion.Version_0_37_0, BackyardDatabaseTables.Version37 },
		};
	} // class
}
