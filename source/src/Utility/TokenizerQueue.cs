using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Timers;

namespace Ginger
{
	public class TokenizerQueue
	{
		private Generator.Output _prompt;
		private int _promptID = 0;
		private Timer _timer = new Timer();

		public struct Result
		{
			public int hash;
			public int tokens_total;
			public int tokens_permanent_faraday;
			public int tokens_permanent_silly;
			public Dictionary<string, int> loreTokens;
		}
		public delegate void OnTokenCount(Result result);
		public event OnTokenCount onTokenCount;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }
		private BackgroundWorker _bgWorker;

		public TokenizerQueue()
		{
			_timer.Interval = 300;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~TokenizerQueue()
		{
			_timer.Dispose();
		}

		public bool Schedule(Generator.Output prompt, int promptID, ISynchronizeInvoke synchronizingObject)
		{
			if (prompt.isEmpty)
			{
				_timer.Stop();
				if (_bgWorker != null)
					_bgWorker.CancelAsync();

				_prompt = prompt;
				_promptID = promptID;
				onTokenCount?.Invoke(new Result() { hash = promptID });
				return false;
			}

			if (promptID == _promptID)
				return false;

			_prompt = prompt;
			_promptID = promptID;

			_timer.Start();
			_timer.Interval = 500;
			_timer.SynchronizingObject = synchronizingObject;
			return true;
		}

		private struct BackgroundWorkerArguments
		{
			public Generator.Output prompt;
			public string characterPlaceholder;
			public string userPlaceholder;
			public int promptHash;
			public AppSettings.Settings.OutputPreviewFormat format;
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (isBusy)
			{
				// Restart the timer
				_timer.Start();
				return;
			}

			var asyncArgs = new BackgroundWorkerArguments() {
				prompt = _prompt,
				characterPlaceholder = Current.Character.namePlaceholder,
				userPlaceholder = Current.Card.userPlaceholder,
				promptHash = _promptID,
				format = AppSettings.Settings.PreviewFormat,
			};

			_bgWorker = new BackgroundWorker();
			_bgWorker.WorkerSupportsCancellation = true;
			_bgWorker.DoWork += BgWorker_DoWork;
			_bgWorker.RunWorkerCompleted += (s, args) => {
				_bgWorker = null;

				if (args.Cancelled || args.Result == null)
					return;

				onTokenCount?.Invoke((Result)args.Result);
			};

			_bgWorker.RunWorkerAsync(asyncArgs);
		}

		private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorkerArguments args = (BackgroundWorkerArguments)e.Argument;

			int total = 0;
			int permanent_silly = 0;
			int permanent_faraday = 0;
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
			Dictionary<string, int> loreTokens = null;
			for (int i = 0; i < numChannels; ++i)
			{
				var channel = EnumHelper.FromInt(i, Recipe.Component.Invalid);
				var sb = new StringBuilder(args.prompt.GetText(channel).ToTavern());
				if (sb.Length == 0)
					continue;

				sb.Replace("{{char}}", args.characterPlaceholder, true);
				sb.Replace("{{user}}", args.userPlaceholder, true);

//				if (args.format == AppSettings.Settings.OutputPreviewFormat.Faraday)
//				{
//					sb.ConvertLinebreaks(Linebreak.LF);
//				}

				var tokens = LlamaTokenizer.LlamaTokenizer.encode(sb.ToString(), true, true);
				total += tokens.Length;
				if (channel == Recipe.Component.System || channel == Recipe.Component.System_PostHistory)
				{
					permanent_faraday += tokens.Length;
				}
				else if (channel == Recipe.Component.Persona
					|| channel == Recipe.Component.UserPersona
					|| channel == Recipe.Component.Scenario)
				{
					permanent_silly += tokens.Length;
					permanent_faraday += tokens.Length;
				}
			}

			if (args.prompt.hasLore)
			{
				loreTokens = new Dictionary<string, int>();
				for (int i = 0; i < args.prompt.lorebook.entries.Count; ++i)
				{
					string key = args.prompt.lorebook.entries[i].GetUID();
					string content = string.Concat(args.prompt.lorebook.entries[i].key, "=", GingerString.FromString(args.prompt.lorebook.entries[i].value).ToTavern());
					var sb = new StringBuilder(content);
					sb.Replace("{{char}}", args.characterPlaceholder, true);
					sb.Replace("{{user}}", args.userPlaceholder, true);

					var tokens = LlamaTokenizer.LlamaTokenizer.encode(sb.ToString(), false, false);
					loreTokens.TryAdd(key, tokens.Length);
				}
			}

			if (((BackgroundWorker)sender).CancellationPending)
			{
				e.Cancel = true;
				return;
			}

			e.Result = new Result() {
				hash = args.promptHash,
				tokens_total = total,
				tokens_permanent_faraday = permanent_faraday,
				tokens_permanent_silly = permanent_silly,
				loreTokens = loreTokens,
			};
		}

	}
}
