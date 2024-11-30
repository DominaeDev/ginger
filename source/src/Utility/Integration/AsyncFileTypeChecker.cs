using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace Ginger
{
	public class AsyncFileTypeChecker
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onComplete;

		private struct WorkerArguments
		{
			public string[] filenames;
		}

		public enum Error
		{
			NoError = 0,
			UnknownError,
			Cancelled,
		}

		public struct Result
		{
			public string[] filenames;
			public Error error;
		}

		private struct Entry
		{
			public string filename;
			public DateTime modifiedDate;
		}

		private struct WorkerResult
		{
			public Entry[] entries;
			public int numChecked;
			public Error error;
		}

		public Queue<string> _queue = new Queue<string>();
		private WorkerResult _result;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private BackgroundWorker _bgWorker;
		private Timer _timer;

		private int _totalCount = 0;
		private int _numChecked = 0;

		private const int IntervalMS = 50;
		private const int BatchSize = 5;

		public AsyncFileTypeChecker()
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~AsyncFileTypeChecker()
		{
			_timer.Dispose();
		}

		public void Enqueue(IEnumerable<string> filenames)
		{
			foreach (var filename in filenames)
				_queue.Enqueue(filename);
		}

		public bool Start()
		{
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = MainForm.instance;
			_timer.Start();

			_totalCount = _queue.Count;
			_numChecked = 0;
			_result = default(WorkerResult);
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

			var filenames = new List<string>(BatchSize);
			for (int i = 0; i < BatchSize && _queue.Count > 0; ++i)
				filenames.Add(_queue.Dequeue());

			var workerArgs = new WorkerArguments() {
				filenames = filenames.ToArray(),
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
			WorkerResult result = new WorkerResult();
			var validEntries = new List<Entry>(BatchSize);
			var filenames = args.filenames;

			int numChecked = 0;
			for (int i = 0; i < filenames.Length; ++i)
			{
				string filename = filenames[i];
				if (FileUtil.CheckFileType(filename) != FileUtil.FileType.Unknown)
				{
					DateTime modifiedDate;
					try
					{
						modifiedDate = new FileInfo(filename).LastWriteTime;
					}
					catch
					{
						continue;
					}

					validEntries.Add(new Entry() {
						filename = filename,
						modifiedDate = modifiedDate,
					});
				}
				numChecked++;

				if (((BackgroundWorker)sender).CancellationPending)
				{
					result.error = Error.Cancelled;
					e.Cancel = true;
					break;
				}
			}

			result.entries = validEntries.ToArray();
			result.numChecked = numChecked;
			e.Result = result;
		}

		private void BgWorker_Completed(object sender, RunWorkerCompletedEventArgs args)
		{
			_bgWorker = null;

			WorkerResult result;
			try
			{
				result = (WorkerResult)args.Result;
			}
			catch
			{
				result = new WorkerResult() {
					entries = new Entry[0],
					error = Error.UnknownError,
				};
			}
			
			_numChecked += result.numChecked;

			if (_result.entries == null)
			{
				_result.entries = new Entry[result.entries.Length];
				Array.Copy(result.entries, _result.entries, result.entries.Length);
			}
			else
				_result.entries = Utility.ConcatenateArrays(_result.entries, result.entries);

			onProgress?.Invoke(100 * _numChecked / _totalCount);

			if (args.Cancelled)
			{
				_result.error = Error.Cancelled;
				onComplete?.Invoke(GetResult());
				return;
			}
			else if (result.error == Error.UnknownError)
			{
				_result.error = Error.UnknownError;
				onComplete?.Invoke(GetResult());
				return;
			}
			else if (_numChecked >= _totalCount || _queue.IsEmpty())
			{
				onComplete?.Invoke(GetResult());
				return;
			}

			// Next
			_timer.Start();
		}

		private Result GetResult()
		{
			return new Result() {
				error = _result.error,
				filenames = _result.entries
					.OrderBy(e => e.modifiedDate)
					.Select(e => e.filename)
					.ToArray(),
			};
		}

		public void Cancel()
		{
			_timer.Stop();
			_queue.Clear();
			if (_bgWorker != null)
				_bgWorker.CancelAsync();
		}

	}
}
