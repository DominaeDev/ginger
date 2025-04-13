using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Linq;

namespace Ginger.Integration
{
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	
	public class BulkUpdateModelSettings
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onComplete;

		private struct WorkerArguments
		{
			public GroupInstance[] groups;
			public ChatParameters chatParameters;
		}

		public enum Error
		{
			NoError = 0,
			Cancelled,

			DatabaseError,
			UnknownError,
		}

		private enum WorkerError
		{
			NoError = 0,
			UnknownError,
			DatabaseError,
		}

		public struct Result
		{
			public int succeeded;
			public int skipped;
			public Error error;
		}

		private struct WorkerResult
		{
			public int skipped;
			public int succeeded;
			public WorkerError error;
		}

		public Queue<GroupInstance> _queue = new Queue<GroupInstance>();
		private ChatParameters _chatParameters;
		public Result _result;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private BackgroundWorker _bgWorker;
		private Timer _timer;

		private int _totalCount = 0;
		private int _completed = 0;

		private const int IntervalMS = 50;
		private const int BatchSize = 20;

		public BulkUpdateModelSettings()
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~BulkUpdateModelSettings()
		{
			_timer.Dispose();
		}

		public void Enqueue(GroupInstance group)
		{
			_queue.Enqueue(group);
		}

		public bool Start(ChatParameters chatParameters)
		{
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = MainForm.instance;
			_timer.Start();

			_totalCount = _queue.Count;
			_completed = 0;
			_result = default(Result);
			_chatParameters = chatParameters;
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

			var groups = new List<GroupInstance>(BatchSize);
			for (int i = 0; i < BatchSize && _queue.Count > 0; ++i)
				groups.Add(_queue.Dequeue());

			var workerArgs = new WorkerArguments() {
				groups = groups.ToArray(),
				chatParameters = _chatParameters,
			};

			_bgWorker = new BackgroundWorker();
			_bgWorker.WorkerSupportsCancellation = true;
			_bgWorker.DoWork += BgWorker_DoWork;
			_bgWorker.RunWorkerCompleted += BgWorker_Completed;

			_bgWorker.RunWorkerAsync(workerArgs);
		}

		private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			WorkerArguments args = (WorkerArguments)e.Argument;
			WorkerResult results = new WorkerResult();

			WorkerError error = WriteModelSettings(args.groups, args.chatParameters);

			switch (error)
			{
			case WorkerError.NoError:
				results.succeeded += args.groups.Length;
				break;
			case WorkerError.UnknownError:
				results.skipped += args.groups.Length;
				break;
			case WorkerError.DatabaseError:
				results.error = error;
				e.Result = results;
				return;
			}

			if (((BackgroundWorker)sender).CancellationPending)
			{
				e.Result = results;
				e.Cancel = true;
				_queue.Clear();
				_timer.Stop();
				return;
			}

			e.Result = results;
		}

		private void BgWorker_Completed(object sender, RunWorkerCompletedEventArgs args)
		{
			_bgWorker = null;

			WorkerResult workResult;
			try
			{
				workResult = (WorkerResult)args.Result;
			}
			catch
			{
				workResult = new WorkerResult() {
					error = WorkerError.UnknownError,
					succeeded = 0,
				};
			}
			
			int count = workResult.succeeded + workResult.skipped;
			_completed += count;

			_result.succeeded += workResult.succeeded;
			_result.skipped += workResult.skipped;

			onProgress?.Invoke(100 * _completed / _totalCount);

			if (args.Cancelled)
			{
				_result.error = Error.Cancelled;
				onComplete?.Invoke(_result);
				return;
			}
			else if (workResult.error == WorkerError.UnknownError)
			{
				_result.error = Error.UnknownError;
				onComplete?.Invoke(_result);
				return;
			}
			else if (workResult.error == WorkerError.DatabaseError)
			{
				_result.error = Error.DatabaseError;
				onComplete?.Invoke(_result);
				return;
			}
			else if (_completed >= _totalCount || _queue.IsEmpty())
			{
				onComplete?.Invoke(_result);
				return;
			}

			// Next
			_timer.Start();
		}

		public void Cancel()
		{
			_timer.Stop();
			_queue.Clear();
			if (_bgWorker != null)
				_bgWorker.CancelAsync();
		}

		private WorkerError WriteModelSettings(GroupInstance[] groups, ChatParameters chatParameters)
		{
			if (Backyard.ConnectionEstablished == false)
				return WorkerError.DatabaseError;

			var lsChats = new List<ChatInstance>();
			foreach (var group in groups)
			{
				ChatInstance[] chats;
				var error = Backyard.Database.GetChats(group.instanceId, out chats);
				if (error == Backyard.Error.SQLCommandFailed || error == Backyard.Error.NotConnected)
					return WorkerError.DatabaseError;
				else if (chats == null || chats.Length == 0)
					return WorkerError.UnknownError;
				lsChats.AddRange(chats);
			}

			if (lsChats.Count == 0)
				return WorkerError.UnknownError;

			if (Backyard.Database.UpdateChatParameters(lsChats.ToArray(), null, chatParameters) == Backyard.Error.NoError)
				return WorkerError.NoError;
			
			return WorkerError.UnknownError;
		}
	}
}
