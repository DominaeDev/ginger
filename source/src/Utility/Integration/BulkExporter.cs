using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Drawing;
using System.IO;
using System.Linq;

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
			public string[] target_filenames;
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
			public List<string> filenames;
			public Error error;
		}

		private struct WorkerResult
		{
			public string[] temp_filenames;
			public string[] dest_filenames;
			public Error error;
		}

		public Queue<KeyValuePair<CharacterInstance, string>> _queue = new Queue<KeyValuePair<CharacterInstance, string>>(); // char, dest filename
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

		public void Enqueue(CharacterInstance characterInstance, string filename)
		{
			_queue.Enqueue(new KeyValuePair<CharacterInstance, string>(characterInstance, filename));
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
			var filenames = new List<string>(BatchSize);
			for (int i = 0; i < BatchSize && _queue.Count > 0; ++i)
			{
				var kvp = _queue.Dequeue();
				instances.Add(kvp.Key);
				filenames.Add(kvp.Value);
			}

			var workerArgs = new WorkerArguments() {
				instances = instances.ToArray(),
				target_filenames = filenames.ToArray(),
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
			results.temp_filenames = new string[args.instances.Length];
			results.dest_filenames = args.target_filenames;

			for (int i = 0; i < args.instances.Length; ++i)
			{
				string intermediateFilename;
				Error error;
				if (args.fileType.Contains(FileUtil.FileType.Backup))
					error = ExportBackup(args.instances[i], out intermediateFilename);
				else
					error = ExportCharacter(args.instances[i], args.fileType, out intermediateFilename);

				if (error == Error.NoError)
				{
					results.temp_filenames[i] = intermediateFilename;
				}
				else
				{
					results.dest_filenames[i] = null;
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
				};
			}

			// Copy files
			List<string> exportedFilenames = new List<string>();
			List<string> removeFilenames = new List<string>();

			// Move intermediate files
			if (workResult.temp_filenames != null && workResult.dest_filenames != null)
			{
				for (int i = 0; i < workResult.temp_filenames.Length && i < workResult.dest_filenames.Length; ++i)
				{
					try
					{
						string tempFilename = workResult.temp_filenames[i];
						string destFilename = workResult.dest_filenames[i];
						if (string.IsNullOrEmpty(tempFilename) == false && File.Exists(tempFilename))
						{
							if (File.Exists(destFilename))
								File.Delete(destFilename);
							File.Move(tempFilename, destFilename);
							exportedFilenames.Add(destFilename);
						}
					}
					catch (IOException e)
					{
						if (e.HResult == Win32.HR_ERROR_DISK_FULL
							|| e.HResult == Win32.HR_ERROR_HANDLE_DISK_FULL)
						{
							removeFilenames.AddRange(workResult.temp_filenames);
							workResult.error = Error.DiskFullError;
							break;
						}
						else if (e.HResult == Win32.HR_ERROR_ACCESS_DENIED
							|| e.HResult == Win32.HR_ERROR_WRITE_PROTECT
							|| e.HResult == Win32.HR_ERROR_FILE_EXISTS)
						{
							removeFilenames.Add(workResult.temp_filenames[i]); // Skip
						}
						else
						{
							removeFilenames.AddRange(workResult.temp_filenames);
							workResult.error = Error.FileError;
							break;
						}
					}
					catch
					{
						removeFilenames.Add(workResult.temp_filenames[i]); // Skip
					}
				}
			}
			
			// Delete temp files
			foreach (var filename in removeFilenames)
			{
				try
				{
					if (string.IsNullOrEmpty(filename) == false && File.Exists(filename))
						File.Delete(filename);
				}
				catch
				{
					// Do nothing
				}
			}

			_completed += exportedFilenames.Count;

			// Combine results
			if (_result.filenames == null)
				_result.filenames = new List<string>();
			_result.filenames.AddRange(exportedFilenames);

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
			ImageInstance[] images;
			var importError = Backyard.ImportCharacter(characterInstance, out faradayCard, out images);
			if (importError != Backyard.Error.NoError)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			// Convert
			var stash = Current.Stash();
			Current.Instance = new GingerCharacter();
			Current.Instance.ReadFaradayCard(faradayCard, null);

			// Load images/backgrounds
			Backyard.Link.Image[] unused;
			Current.ImportImages(images, out unused);

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
				Current.Restore(stash);
			}
		}

		private Error ExportBackup(CharacterInstance characterInstance, out string filename)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			BackupData backupData;
			var importError = BackupUtil.CreateBackup(characterInstance, out backupData);
			if (importError != Backyard.Error.NoError)
			{
				filename = default(string);
				return Error.DatabaseError;
			}

			try
			{
				string intermediateFilename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				var writeError = BackupUtil.WriteBackup(intermediateFilename, backupData);

				// Write to disk
				if (writeError == FileUtil.Error.NoError)
				{
					filename = intermediateFilename;
					return Error.NoError;
				}
				else if (writeError == FileUtil.Error.DiskFullError)
				{
					filename = default(string);
					return Error.DiskFullError;
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
		}
	}
}
