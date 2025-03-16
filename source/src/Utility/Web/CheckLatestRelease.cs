using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace Ginger
{
	public static class CheckLatestRelease
	{
		public struct ReleaseInfo
		{
			public VersionNumber version;
			public string url;
			public bool success;
		}
		public delegate void Response(ReleaseInfo releaseInfo);

		private static readonly string LatestReleaseEndpointUri = "https://api.github.com/repos/dominaedev/ginger/releases/latest";

		public static void GetLatestRelease(Response onComplete)
		{
			// Spin up background worker
			var _bgWorker = new BackgroundWorker();
			_bgWorker.WorkerSupportsCancellation = true;
			_bgWorker.DoWork += BgWorker_DoWork;
			_bgWorker.RunWorkerCompleted += (s, args) => {
				if (args.Cancelled)
					return;

				onComplete?.Invoke((ReleaseInfo)args.Result);
			};

			_bgWorker.RunWorkerAsync();
		}

		private static void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(LatestReleaseEndpointUri);
			httpRequest.UserAgent = string.Format("Ginger/{0}.{1}.{2}", VersionNumber.Application.Major, VersionNumber.Application.Minor, VersionNumber.Application.Build);
			httpRequest.Accept = "application/vnd.github+json";
			httpRequest.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
			httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			try
			{
				using (HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						using (StreamReader reader = new StreamReader(stream))
						{
							ReleaseInfo info;
							string sResponse = reader.ReadToEnd();
							ParseResponse(sResponse, out info);
							e.Result = info;
						}
					}
				}
			}
			catch
			{
				e.Result = new ReleaseInfo() {
					success = false,
				};
			}
		}

		private static bool ParseResponse(string response, out ReleaseInfo info)
		{
			LatestReleaseJson data = LatestReleaseJson.FromJson(response);
			if (data == null)
			{
				info = default(ReleaseInfo);
				return false;
			}

			info = new ReleaseInfo() {
				version = data.ParseVersion(),
				url = data.url,
				success = true,
			};
			return true;
		}
	}
}
