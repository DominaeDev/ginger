using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Ginger.Integration;

namespace Ginger
{
	static class Program {

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args) 
		{
			if (args.Length > 0 && args[0] == "--setup")
			{
				// Run post-setup step, then exit.
				return PostInstall.Execute();
			}

			if (Environment.Is64BitProcess)
				AppDomain.CurrentDomain.AppendPrivatePath("Libraries/x64");
			else
				AppDomain.CurrentDomain.AppendPrivatePath("Libraries/x86");

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Locales.Init();
			Dictionaries.Load();
			AppSettings.LoadFromIni(Utility.AppPath("Settings.ini"));

			var mainForm = new MainForm();
			if (args.Length > 0 && File.Exists(args[0]))
				MainForm.instance.SetFirstLoad(args[0]);

			// Initialize link
			if (AppSettings.BackyardLink.Enabled)
			{
				// Check last version
				VersionNumber appVersion;
				if (Backyard.GetAppVersion(out appVersion) && AppSettings.BackyardLink.LastVersion != appVersion)
					AppSettings.BackyardLink.Enabled = false; // Do not auto-connect to newer versions
				else
				{
					if (Backyard.EstablishConnection() == Backyard.Error.NoError)
						Backyard.RefreshCharacters();
					else
						AppSettings.BackyardLink.Enabled = false;
				}
			}

			Application.Run(mainForm);

			// Clean up
			SpellChecker.Release();
			DefaultPortrait.Dispose();

			return 0;
		}

		private readonly static Dictionary<string, Assembly> _libs = new Dictionary<string, Assembly>();

		private readonly static Dictionary<string, string> _libAliases = new Dictionary<string, string>() {
			{ "DarkNet.dll", "DarkMode.dll" }, // DarkNet sounds kind of malicious
		};

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string keyName = new AssemblyName(args.Name).Name;
			if (keyName.Contains(".resources"))
				return null;

			Assembly assembly;
			if (_libs.TryGetValue(keyName, out assembly))
				return assembly;

			string dllName = DllResourceName(keyName);

			string alias;
			if (_libAliases.TryGetValue(dllName, out alias))
				dllName = alias;

			List<string> searchPaths = new List<string>() {
				"",
			};
			if (Environment.Is64BitProcess)
				searchPaths.Add("Libraries/x64");
			else
				searchPaths.Add("Libraries/x86");

			string filename = null;
			foreach (var searchPath in searchPaths)
			{
				if (File.Exists(Utility.AppPath(searchPath, dllName)))
				{
					filename = Utility.AppPath(searchPath, dllName);
					break;
				}
			}
			if (filename == null)
				return null; // Not found

			if (filename != null)
			{
				using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					if (stream == null)
						return null;

					byte[] buffer = new BinaryReader(stream).ReadBytes((int)stream.Length);
					assembly = Assembly.Load(buffer);

					_libs[keyName] = assembly;
					return assembly;
				}
			}
			else
			{
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(dllName))
				{
					if (stream == null)
						return null;

					byte[] buffer = new BinaryReader(stream).ReadBytes((int)stream.Length);
					assembly = Assembly.Load(buffer);

					_libs[keyName] = assembly;
					return assembly;
				}
			}
		}

		private static string DllResourceName(string dllName)
		{
			if (dllName.Contains(".dll") == false) 
				dllName += ".dll";

			foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
			{
				if (name.EndsWith(dllName)) 
					return name;
			}
			return dllName;
		}
	}
}
