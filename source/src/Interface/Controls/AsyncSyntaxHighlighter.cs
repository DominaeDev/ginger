using System.ComponentModel;
using System.Timers;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public class AsyncSyntaxHighlighter
	{
		public delegate void OnResult(Result result);
		public event OnResult onResult;

		private struct Arguments
		{
			public string unformattedText;
			public TextSpans spelling;
			public SyntaxHighlighter syntaxHighlighter;
			public int hash;
		}

		public struct Result
		{
			public string unformattedText;
			public string rtfText;
			public int hash;
		}

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private Arguments _arguments;
		private BackgroundWorker _bgWorker;
		private Timer _timer;
		private int _lastHash = 0;

		private const int IntervalMS = 50;

		public AsyncSyntaxHighlighter()
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~AsyncSyntaxHighlighter()
		{
			_timer.Dispose();
		}

		public bool Schedule(RichTextBoxEx textBox)
		{
			string text = textBox.Text;
			if (string.IsNullOrEmpty(text) || textBox.syntaxHighlighter == null)
			{
				// Restart timer
				_timer.Stop();
				if (_bgWorker != null)
					_bgWorker.CancelAsync();
				return false;
			}

			int hash = text.GetHashCode();
			if (hash == _lastHash)
				return false;

			_lastHash = hash;
			_arguments = new Arguments() {
				unformattedText = text,
				syntaxHighlighter = textBox.syntaxHighlighter,
				spelling = textBox.textSpans,
				hash = hash,
			};

			_timer.Start();
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = textBox;
			return true;
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (isBusy)
			{
				// Restart the timer
				_timer.Start();
				return;
			}

			_bgWorker = new BackgroundWorker();
			_bgWorker.WorkerSupportsCancellation = true;
			_bgWorker.DoWork += BgWorker_DoWork;
			_bgWorker.RunWorkerCompleted += (s, args) => {
				_bgWorker = null;
				_lastHash = 0;
				if (args.Cancelled)
					return;

				onResult?.Invoke((Result)args.Result);
			};

			_bgWorker.RunWorkerAsync(_arguments);
		}

		private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			Arguments args = (Arguments)e.Argument;

			args.syntaxHighlighter.SetSpellChecking(args.spelling);
			string rtf = args.syntaxHighlighter.Highlight(args.unformattedText);

			if (((BackgroundWorker)sender).CancellationPending)
			{
				e.Cancel = true;
				return;
			}

			e.Result = new Result() {
				rtfText = rtf,
				unformattedText = args.unformattedText,
				hash = args.hash,
			};
		}

		public void Cancel()
		{
			// Restart timer
			_timer.Stop();
			if (_bgWorker != null)
				_bgWorker.CancelAsync();
		}

		public void Invalidate()
		{
			_lastHash = 0;
		}
	}
}
