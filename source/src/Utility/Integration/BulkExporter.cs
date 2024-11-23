using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Ginger.Integration
{
	public class BulkExporter
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onResult;

		private struct WorkerArguments
		{
			public CharacterInstance[] instances;
			public FileUtil.FileType fileType;
		}

		public enum Error
		{
			NoError,
			Cancelled,
			UnknownError,
			DatabaseError,
			FileError,
		}

		public struct Result
		{
			public string[] filename;
			public int succeeded;
			public Error error;
		}

		public Queue<CharacterInstance> _queue = new Queue<CharacterInstance>();
		public List<Result> _results = new List<Result>();

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private WorkerArguments _arguments;
		private BackgroundWorker _bgWorker;
		private Timer _timer;
		private FileUtil.FileType _fileType = FileUtil.FileType.Unknown;

		private const int IntervalMS = 50;
		private const int BatchSize = 10;

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
			_bgWorker.RunWorkerCompleted += (s, args) => {
				_bgWorker = null;
				if (args.Cancelled)
					return;


				onResult?.Invoke((Result)args.Result);
			};

			_bgWorker.RunWorkerAsync(workerArgs);
		}

		private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			WorkerArguments args = (WorkerArguments)e.Argument;

			for (int i = 0; i < args.instances.Length; ++i)
			{
				string intermediateFilename;
				Error error = ExportCharacter(args.instances[i], args.fileType, out intermediateFilename);

				if (error != Error.NoError)
				{
					e.Result = new Result() {
						error = error,
					};
					return;
				}

				if (((BackgroundWorker)sender).CancellationPending)
				{
					e.Cancel = true;
					return;
				}
			}

			e.Result = new Result();
		}

		public void Cancel()
		{
			_timer.Stop();
			if (_bgWorker != null)
				_bgWorker.CancelAsync();

			onResult?.Invoke(new Result() {
				error = Error.Cancelled,
			});
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
			var importError = Backyard.ImportCharacter(characterInstance, out faradayCard, out imageUrls);
			if (importError != Backyard.Error.NoError)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			// Read image
			Image image = null;
			if (imageUrls != null && imageUrls.Length > 0)
				Utility.LoadImageFromFile(imageUrls[0], out image);

			// Convert
			GingerCharacter character = new GingerCharacter();
			character.ReadFaradayCard(faradayCard, image);

			// Write to disk
			string intermediateFilename = Path.GetTempFileName();
			if (FileUtil.Export(character, intermediateFilename, fileType))
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
		
	}
}
