using System;
using System.IO;
using System.Windows.Forms;

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

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Locales.Init();
			Dictionaries.Load();
			AppSettings.LoadFromIni(Utility.AppPath("Settings.ini"));

			var mainForm = new MainForm();
			if (args.Length > 0 && File.Exists(args[0]))
				MainForm.instance.SetFirstLoad(args[0]);

			// Initialize link
			if (AppSettings.FaradayLink.Enabled)
			{
				if (FaradayBridge.EstablishConnection() == FaradayBridge.Error.NoError)
					FaradayBridge.RefreshCharacters();
				else
					AppSettings.FaradayLink.Enabled = false;
			}

			Application.Run(mainForm);

			// Clean up
			SpellChecker.Release();
			DefaultPortrait.Dispose();

			return 0;
		}
	}
}
