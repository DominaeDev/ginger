using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace Ginger
{
	public interface IAsyncTask<TArgs, TResult>
	{
		bool Execute(TArgs args, out TResult result);
		void Progress(int percent);
		void Complete(AsyncTaskQueue<TArgs, TResult>.Error error, TResult[] results, int completed, int total);

		Action<int> onProgress { get; set; }
		Action<AsyncTaskQueue<TArgs, TResult>.Error, TResult[], int, int> onComplete { get; set; }
	}

	public class AsyncTaskQueue<TArgs, TResult>
	{
		private struct WorkerArguments
		{
			public TArgs[] args;
		}

		public enum Error
		{
			NoError = 0,
			Cancelled = 1,
			Error,
		}

		public struct Result
		{
			public TResult[] results;
			public Error error;
		}

		private struct WorkerResult
		{
			public TResult[] results;
			public Error error;
			public int numChecked;
		}

		public Queue<TArgs> _queue = new Queue<TArgs>();
		private WorkerResult _result;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private BackgroundWorker _bgWorker;
		private Timer _timer;

		private int _totalCount = 0;
		private int _numChecked = 0;
		private bool _haltOnError = true;

		private const int IntervalMS = 50;
		public int BatchSize = 5;

		private IAsyncTask<TArgs, TResult> _task;

		public AsyncTaskQueue(IAsyncTask<TArgs, TResult> task)
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;

			_task = task;
		}

		~AsyncTaskQueue()
		{
			_timer.Dispose();
		}

		public void Enqueue(TArgs args)
		{
			_queue.Enqueue(args);
		}

		public void Enqueue(IEnumerable<TArgs> args)
		{
			foreach (var arg in args)
				_queue.Enqueue(arg);
		}

		public bool Start(bool haltOnError = true)
		{
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = MainForm.instance;
			_timer.Start();

			_totalCount = _queue.Count;
			_numChecked = 0;
			_result = default(WorkerResult);
			_haltOnError = haltOnError;
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

			var filenames = new List<TArgs>(BatchSize);
			for (int i = 0; i < BatchSize && _queue.Count > 0; ++i)
				filenames.Add(_queue.Dequeue());

			var workerArgs = new WorkerArguments() {
				args = filenames.ToArray(),
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
			WorkerResult workerResult = new WorkerResult();

			var taskArgs = args.args;
			List<TResult> taskResults = new List<TResult>();

			int numChecked = 0;
			for (int i = 0; i < taskArgs.Length; ++i)
			{
				var taskArg = taskArgs[i];

				TResult result;
				bool bSuccess = _task.Execute(taskArg, out result);
				numChecked++;
				if (bSuccess)
					taskResults.Add(result);
				else if (_haltOnError)
				{
					workerResult.error = Error.Error;
					break;
				}

				if (((BackgroundWorker)sender).CancellationPending)
				{
					workerResult.error = Error.Cancelled;
					e.Cancel = true;
					break;
				}
			}

			workerResult.results = taskResults.ToArray();
			workerResult.numChecked = numChecked;
			e.Result = workerResult;
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
					results = new TResult[0],
					error = Error.Error,
				};
			}
			
			_numChecked += result.numChecked;

			// Copy results
			if (_result.results == null)
			{
				_result.results = new TResult[result.results.Length];
				Array.Copy(result.results, _result.results, result.results.Length);
			}
			else
				_result.results = Utility.ConcatenateArrays(_result.results, result.results);

			// Report progress
			_task.Progress(100 * _numChecked / _totalCount);

			if (args.Cancelled)
			{
				_task.Complete(Error.Cancelled, _result.results, _numChecked, _totalCount);
				return;
			}
			else if (result.error == Error.Error)
			{
				_task.Complete(Error.Error, _result.results, _numChecked, _totalCount);
				return;
			}
			else if (_numChecked >= _totalCount || _queue.IsEmpty())
			{
				_task.Complete(Error.NoError, _result.results, _numChecked, _totalCount);
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
	}

	public abstract class AsyncTask<TWorker,A,R> where TWorker : IAsyncTask<A,R>, new()
	{
		private TWorker _worker;
		private AsyncTaskQueue<A, R> _queue;

		protected struct AsyncResult
		{
			public AsyncTaskQueue<A, R>.Error error;
			public R[] results;
			public int count;
			public int total;
		}

		public AsyncTask()
		{
			_worker = new TWorker();
			_worker.onProgress = (p) => {
				Progress(p);
			};
			_worker.onComplete = (e, r, n, t) => {
				Completed(new AsyncResult() {
					error = e,
					results = r,
					count = n,
					total = t,
				});
			};
			_queue = new AsyncTaskQueue<A, R>(_worker);
		}

		protected abstract void Progress(int percent);
		protected abstract void Completed(AsyncResult result);

		public virtual void Enqueue(IEnumerable<A> args)
        {
            _queue.Enqueue(args);
        }

		public virtual void Start()
		{
			_queue.Start(false);
		}

		public virtual void Cancel()
		{
			_queue.Cancel();
		}
	}

	public abstract class AsyncWorkerBase<A, R> : IAsyncTask<A, R>
	{
		public Action<int> onProgress { get; set; }
		public Action<AsyncTaskQueue<A, R>.Error, R[], int, int> onComplete  { get; set; }

		public void Progress(int percent)
		{
			onProgress?.Invoke(percent);
		}

		public void Complete(AsyncTaskQueue<A, R>.Error error, R[] results, int completed, int total)
		{
			onComplete?.Invoke(error, results, completed, total);
		}

		public abstract bool Execute(A args, out R result);
	}
}
