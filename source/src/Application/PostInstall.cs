using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace Ginger
{
	public static class PostInstall
	{
		// This is called by the Ginger installer (via command-line argument) at the end of the setup process.
		// It sets important read/write permissions on the Ginger folder and subfolders for all local users.
		// Otherwise, if the user installs Ginger to their C:\Program files folder, Ginger won't be able
		// to save it settings or create new recipes.

		[SecurityPermission(SecurityAction.Demand)]
		public static int Execute()
		{
			try
			{
				// Get the installation folder
				string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

				// This gets the "Authenticated Users" group, no matter what it's called
				SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);

				// Create the access rules
				FileSystemAccessRule writeRule1 = new FileSystemAccessRule(sid, FileSystemRights.Modify, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
				FileSystemAccessRule writeRule2 = new FileSystemAccessRule(sid, FileSystemRights.Modify, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);

				if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
				{
					// Get your file's ACL
					DirectorySecurity fsecurity = Directory.GetAccessControl(folder);

					// Add the new rule to the ACL
					fsecurity.AddAccessRule(writeRule1);
					fsecurity.AddAccessRule(writeRule2);

					// Set the ACL back to the file
					Directory.SetAccessControl(folder, fsecurity);
				}
			}
			catch
			{
				// Welp!
				return 1;
			}
			return 0;
		}
	}
}