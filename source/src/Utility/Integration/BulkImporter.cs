﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Drawing;
using System.IO;

namespace Ginger.Integration
{
	public class BulkImporter
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onComplete;

		private struct WorkerArguments
		{
			public string[] filenames;
			public FolderInstance parentFolder;
		}

		public enum Error
		{
			NoError = 0,
			Skipped,
			Cancelled,

			DatabaseError,
			UnknownError,
		}

		private enum WorkerError
		{
			NoError = 0,
			Skipped,

			UnknownError,
			DatabaseError,
		}

		public struct Result
		{
			public CharacterInstance[] characters;
			public int skipped;
			public int succeeded;
			public Error error;
		}

		private struct WorkerResult
		{
			public CharacterInstance[] instances;
			public int skipped;
			public int succeeded;
			public WorkerError error;
		}

		public Queue<string> _queue = new Queue<string>();
		public Result _result;

		private bool isBusy { get { return _bgWorker != null && _bgWorker.IsBusy; } }

		private BackgroundWorker _bgWorker;
		private Timer _timer;
		private FolderInstance _parentFolder;

		private int _totalCount = 0;
		private int _completed = 0;

		private const int IntervalMS = 50;
		private const int BatchSize = 5;

		public BulkImporter()
		{
			_timer = new Timer();
			_timer.Interval = IntervalMS;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
		}

		~BulkImporter()
		{
			_timer.Dispose();
		}

		public void Enqueue(string filename)
		{
			_queue.Enqueue(filename);
		}

		public bool Start(FolderInstance folder)
		{
			_timer.Interval = IntervalMS;
			_timer.SynchronizingObject = MainForm.instance;
			_timer.Start();

			_totalCount = _queue.Count;
			_completed = 0;
			_result = default(Result);
			_parentFolder = folder;
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
				parentFolder = _parentFolder,
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
			results.instances = new CharacterInstance[args.filenames.Length];

			for (int i = 0; i < args.filenames.Length; ++i)
			{
				CharacterInstance importedCharacter;
				WorkerError error = ImportCharacter(args.filenames[i], args.parentFolder, out importedCharacter);

				switch (error)
				{
				case WorkerError.NoError:
					results.instances[i] = importedCharacter;
					results.succeeded += 1;
					break;
				case WorkerError.Skipped:
				case WorkerError.UnknownError:
					results.skipped += 1;
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
					instances = new CharacterInstance[0],
					succeeded = 0,
				};
			}
			
			int count = workResult.instances.Length;
			_completed += count;

			if (_result.characters == null)
			{
				_result.characters = new CharacterInstance[workResult.instances.Length];
				Array.Copy(workResult.instances, _result.characters, workResult.instances.Length);
			}
			else
				_result.characters = Utility.ConcatenateArrays(_result.characters, workResult.instances);
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

		private WorkerError ImportCharacter(string filename, FolderInstance parentFolder, out CharacterInstance characterInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				characterInstance = default(CharacterInstance);
				return WorkerError.DatabaseError;
			}

			GingerCharacter prevInstance = Current.Instance;
			Current.Instance = new GingerCharacter();
			Current.Instance.Reset();

			try
			{

				string ext = (Path.GetExtension(filename) ?? "").ToLowerInvariant();

				int jsonErrors = 0;
				FileUtil.Error error;
				if (ext == ".png")
					error = FileUtil.ImportCharacterFromPNG(filename, out jsonErrors, FileUtil.Format.SillyTavernV2 | FileUtil.Format.SillyTavernV3 | FileUtil.Format.Faraday);
				else if (ext == ".json")
					error = FileUtil.ImportCharacterJson(filename, out jsonErrors);
				else if (ext == ".charx")
					error = FileUtil.ImportCharacterFromPNG(filename, out jsonErrors, FileUtil.Format.SillyTavernV3);
				else if (ext == ".yaml")
				{
					error = FileUtil.ImportCharacterJson(filename, out jsonErrors);

					// Load portrait image (if any)
					if (error == FileUtil.Error.NoError)
					{
						var pngFilename = Path.Combine(Path.GetDirectoryName(filename), string.Concat(Path.GetFileNameWithoutExtension(filename), ".png"));

						Image image;
						if (Utility.LoadImageFromFile(pngFilename, out image))
							Current.Card.portraitImage = ImageRef.FromImage(image);
					}
				}
				else
					error = FileUtil.Error.UnrecognizedFormat;

				if (!(error == FileUtil.Error.NoError || error == FileUtil.Error.FallbackError))
				{
					characterInstance = default(CharacterInstance);
					return WorkerError.Skipped;
				}

				// Write character to Backyard
				FaradayCardV4 card = FaradayCardV4.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.Faraday));
				
				// Portrait image
				Backyard.ImageInput[] imageInput;
				if (Current.Card.portraitImage != null)
				{
					imageInput = new Backyard.ImageInput[] {
						new Backyard.ImageInput() {
							image = Current.Card.portraitImage,
							fileExt = "png",
						}
					};
				}
				else
				{
					imageInput = new Backyard.ImageInput[] {
						new Backyard.ImageInput() {
							image = DefaultPortrait.Image,
							fileExt = "png",
						}
					};
				}

				Backyard.Link.Image[] imageLinks; // Ignored
				var writeError = Backyard.CreateNewCharacter(card, imageInput, null, out characterInstance, out imageLinks, parentFolder);
				if (writeError != Backyard.Error.NoError)
				{
					characterInstance = default(CharacterInstance);
					return WorkerError.DatabaseError;
				}

				return WorkerError.NoError;
			}
			finally
			{
				Current.Instance = prevInstance;
			}
		}
	}
}