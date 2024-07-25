using Ginger.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public static class LaunchTextEditor
	{
		#region Win32 interop
		[DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
		public static extern uint AssocQueryString(
			AssocF flags,
			AssocStr str,
			string pszAssoc,
			string pszExtra,
			[Out] StringBuilder pszOut,
			ref uint pcchOut
		);

		[Flags]
		public enum AssocF
		{
			None = 0,
			Init_NoRemapCLSID = 0x1,
			Init_ByExeName = 0x2,
			Open_ByExeName = 0x2,
			Init_DefaultToStar = 0x4,
			Init_DefaultToFolder = 0x8,
			NoUserSettings = 0x10,
			NoTruncate = 0x20,
			Verify = 0x40,
			RemapRunDll = 0x80,
			NoFixUps = 0x100,
			IgnoreBaseClass = 0x200,
			Init_IgnoreUnknown = 0x400,
			Init_Fixed_ProgId = 0x800,
			Is_Protocol = 0x1000,
			Init_For_File = 0x2000
		}

		public enum AssocStr
		{
			Command = 1,
			Executable,
			FriendlyDocName,
			FriendlyAppName,
			NoOpen,
			ShellNewValue,
			DDECommand,
			DDEIfExec,
			DDEApplication,
			DDETopic,
			InfoTip,
			QuickTip,
			TileInfo,
			ContentType,
			DefaultIcon,
			ShellExtension,
			DropTarget,
			DelegateExecute,
			Supported_Uri_Protocols,
			ProgID,
			AppID,
			AppPublisher,
			AppIconReference,
			Max
		}
		#endregion

		private static string _defaultApp = null;

		static LaunchTextEditor()
		{
			try
			{
				_defaultApp = AssocQueryString(AssocStr.Executable, ".txt");
			}
			catch
			{
				_defaultApp = null;
			}
		}


		public static void OpenTextFile(string filename)
		{
			if (File.Exists(filename) == false)
			{
				MessageBox.Show(Resources.error_file_not_found, Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				var argument = Path.Combine(Directory.GetCurrentDirectory(), filename);
				argument = argument.Replace("/", "\\");
				if (argument.Contains(" "))
					argument = string.Concat("\"", argument, "\"");

				var processInfo = new ProcessStartInfo() {
					FileName = _defaultApp ?? "explorer",
					Arguments = argument,
					UseShellExecute = true,
					
				};
				Process.Start(processInfo);
			}
			catch
			{
				MessageBox.Show(Resources.error_launch_text_editor, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		public static void OpenAnyFile(string filename)
		{
			if (File.Exists(filename) == false)
			{
				MessageBox.Show(Resources.error_file_not_found, Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				var argument = filename.Replace("/", "\\");
				if (argument.Contains(" "))
					argument = string.Concat("\"", argument, "\"");

				var processInfo = new ProcessStartInfo() {
					FileName = "explorer",
					Arguments = argument,
					UseShellExecute = true,
				};
				Process.Start(processInfo);
			}
			catch
			{
				MessageBox.Show(Resources.error_open_file_in_exporer, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		static string AssocQueryString(AssocStr association, string extension)
		{
			const int S_OK = 0;
			const int S_FALSE = 1;

			uint length = 0;
			uint ret = AssocQueryString(AssocF.None, association, extension, null, null, ref length);
			if (ret != S_FALSE)
			{
				throw new InvalidOperationException("Could not determine associated string");
			}

			var sb = new StringBuilder((int)length); // (length-1) will probably work too as the marshaller adds null termination
			ret = AssocQueryString(AssocF.None, association, extension, null, sb, ref length);
			if (ret != S_OK)
			{
				throw new InvalidOperationException("Could not determine associated string");
			}

			return sb.ToString();
		}
	}
}
