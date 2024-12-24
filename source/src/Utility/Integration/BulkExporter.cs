using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Drawing;
using System.IO;

namespace Ginger.Integration
{
	public class BulkExporter
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onComplete;

		private struct WorkerArguments
		{
			public CharacterInstance[] instances;
			public FileUtil.FileType fileType;
		}

		public enum Error
		{
			NoError = 0,
			Cancelled,
			UnknownError,
			DatabaseError,
			FileError,
			DiskFullError,
		}

		public struct Result
		{
			public string[] filenames;
			public int succeeded;
			public Error error;
		}

		private struct WorkerResult
		{
			public string[] filenames;
			public int succeeded;
			public Error error;
		}

		public Queue<CharacterInstance> _queue = new Queue<CharacterInstance>();
		public Result _result;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private BackgroundWorker _bgWorker;
		private Timer _timer;
		private FileUtil.FileType _fileType = FileUtil.FileType.Unknown;

		private int _totalCount = 0;
		private int _completed = 0;

		private const int IntervalMS = 50;
		private const int BatchSize = 5;

		public BulkExporter()
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~BulkExporter()
		{
			_timer.Dispose();
		}

		public void Enqueue(CharacterInstance characterInstance)
		{
			_queue.Enqueue(characterInstance);
		}

		public bool Start(FileUtil.FileType fileType)
		{
			if (fileType == FileUtil.FileType.Unknown)
				return false;
		
			_fileType = fileType;
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = MainForm.instance;
			_timer.Start();

			_totalCount = _queue.Count;
			_completed = 0;
			_result = default(Result);
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

			var instances = new List<CharacterInstance>(BatchSize);
			for (int i = 0; i < BatchSize && _queue.Count > 0; ++i)
				instances.Add(_queue.Dequeue());

			var workerArgs = new WorkerArguments() {
				instances = instances.ToArray(),
				fileType = _fileType,
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
			results.filenames = new string[args.instances.Length];

			for (int i = 0; i < args.instances.Length; ++i)
			{
				string exportedFilename;
				Error error = ExportCharacter(args.instances[i], args.fileType, out exportedFilename);

				if (error == Error.NoError)
				{
					results.succeeded += 1;
					results.filenames[i] = exportedFilename;
				}
				else
				{
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
					error = Error.UnknownError,
					filenames = new string[0],
					succeeded = 0,
				};
			}
				

			int count = workResult.filenames.Length;
			_completed += count;

			if (_result.filenames == null)
			{
				_result.filenames = new string[workResult.filenames.Length];
				Array.Copy(workResult.filenames, _result.filenames, workResult.filenames.Length);
			}
			else
				_result.filenames = Utility.ConcatenateArrays(_result.filenames, workResult.filenames);
			_result.succeeded += workResult.succeeded;

			onProgress?.Invoke(100 * _completed / _totalCount);

			if (args.Cancelled)
			{
				_result.error = Error.Cancelled;
				onComplete?.Invoke(_result);
				return;
			}
			else if (workResult.error != Error.NoError)
			{
				_result.error = workResult.error;
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

		private Error ExportCharacter(CharacterInstance characterInstance, FileUtil.FileType fileType, out string filename)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			// Read character from Backyard
			FaradayCardV4 faradayCard;
			string[] imageUrls;
			string[] backgroundUrls;
			var importError = Backyard.ImportCharacter(characterInstance, out faradayCard, out imageUrls, out backgroundUrls);
			if (importError != Backyard.Error.NoError)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			// Convert
			GingerCharacter character = new GingerCharacter();
			character.ReadFaradayCard(faradayCard, null);

			var prevInstance = Current.Instance;
			Current.Instance = character;

			// Load images/backgrounds
			Backyard.Link.Image[] unused;
			Current.ImportImages(imageUrls, out unused, AssetFile.AssetType.Icon);
			
			if (backgroundUrls != null && backgroundUrls.Length > 0)
				Current.ImportImages(backgroundUrls, out unused, AssetFile.AssetType.Background);

			try
			{
				// Write to disk
				string intermediateFilename = Path.GetTempFileName();
				if (FileUtil.Export(intermediateFilename, fileType))
				{
					filename = intermediateFilename;
					return Error.NoError;
				}
				else
				{
					filename = default(string);
					return Error.FileError;
				}
			}
			catch (IOException e)
			{
				filename = default(string);
				if (e.HResult == Win32.HR_ERROR_DISK_FULL || e.HResult == Win32.HR_ERROR_HANDLE_DISK_FULL)
					return Error.DiskFullError;
				return Error.FileError;
			}
			catch (Exception e)
			{
				filename = default(string);
				return Error.UnknownError;
			}
			finally
			{
				Current.Instance = prevInstance;
			}
		}
		
	}
}
