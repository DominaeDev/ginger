using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Ginger.Integration
{
	using GroupInstance = Backyard.GroupInstance;
	using CharacterInstance = Backyard.CharacterInstance;
	using FolderInstance = Backyard.FolderInstance;

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
			public int succeeded;
			public int skipped;
			public int groups;
			public Error error;
		}

		private struct WorkerResult
		{
			public CharacterInstance[] instances;
			public int succeeded;
			public int skipped;
			public int groups;
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
			results.instances = new CharacterInstance[0];

			List<CharacterInstance> characterInstances = new List<CharacterInstance>();

			for (int i = 0; i < args.filenames.Length; ++i)
			{
				CharacterInstance[] importedCharacters;
				WorkerError error;
				if (Utility.GetFileExt(args.filenames[i]) == "zip")
					error = ImportBackup(args.filenames[i], args.parentFolder, out importedCharacters);
				else
				{
					CharacterInstance importedCharacter;
					error = ImportCharacter(args.filenames[i], args.parentFolder, out importedCharacter);
					importedCharacters = new CharacterInstance[1] { importedCharacter };
				}

				if (importedCharacters.Length > 1)
					results.groups += 1;

				switch (error)
				{
				case WorkerError.NoError:
					characterInstances.AddRange(importedCharacters);
					results.succeeded += importedCharacters.Length;
					break;
				case WorkerError.Skipped:
				case WorkerError.UnknownError:
					results.skipped += importedCharacters.Length;
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

			results.instances = characterInstances.ToArray();
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
				_result.characters = Utility.ConcatArrays(_result.characters, workResult.instances);
			_result.succeeded += workResult.succeeded;
			_result.skipped += workResult.skipped;
			_result.groups += workResult.groups;

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

			var stash = Current.Stash();

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
				var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);
				BackyardLinkCard card = BackyardLinkCard.FromOutput(output);
				
				card.EnsureSystemPrompt(false);

				Backyard.ImageInput[] imageInput = BackyardUtil.GatherImages();
				BackupData.Chat[] chats = null;

				if (AppSettings.BackyardLink.ImportAlternateGreetings && output.greetings.Length > 1)
					chats = BackupUtil.SplitAltGreetings(card, output.alternativeGreetings, imageInput);

				var args = new Backyard.CreateCharacterArguments() {
					card = card,
					imageInput = imageInput,
					chats = chats,
					folder = parentFolder,
				};
				Backyard.Link.Image[] imageLinks; // Ignored
				var writeError = Backyard.Database.CreateNewCharacter(args, out characterInstance, out imageLinks);
				if (writeError != Backyard.Error.NoError)
				{
					characterInstance = default(CharacterInstance);
					return WorkerError.DatabaseError;
				}

				return WorkerError.NoError;
			}
			finally
			{
				Current.Restore(stash);
			}
		}

		private WorkerError ImportBackup(string filename, FolderInstance parentFolder, out CharacterInstance[] characterInstances)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				characterInstances = null;
				return WorkerError.DatabaseError;
			}

			BackupData backup;
			var readError = BackupUtil.ReadBackup(filename, out backup);
			if (readError != FileUtil.Error.NoError || backup.characterCards == null || backup.characterCards.Length == 0)
			{
				characterInstances = null;
				return WorkerError.Skipped;
			}

			List<Backyard.ImageInput> images = new List<Backyard.ImageInput>();
				
			images.AddRange(backup.images
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						actorIndex = i.characterIndex,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Icon,
					},
					fileExt = i.ext,
				}));

			images.AddRange(backup.backgrounds
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Background,
					},
					fileExt = i.ext,
				}));

			if (backup.userPortrait != null && backup.userPortrait.data != null && backup.userPortrait.data.Length > 0)
			{
				images.Add(new Backyard.ImageInput {
					asset = new AssetFile() {
						name = backup.userPortrait.filename,
						data = AssetData.FromBytes(backup.userPortrait.data),
						ext = backup.userPortrait.ext,
						assetType = AssetFile.AssetType.UserIcon,
					},
					fileExt = backup.userPortrait.ext,
				});
			}

			// Use default model settings
			foreach (var chat in backup.chats)
				chat.parameters = AppSettings.BackyardSettings.UserSettings;

			var cards = backup.characterCards.Select(c => BackyardLinkCard.FromFaradayCard(c)).ToArray();
			cards[0].EnsureSystemPrompt(cards.Length > 0);

			Backyard.Error error;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.GroupChat))
			{
				// Write group to database
				var args = new Backyard.CreatePartyArguments() {
					cards = cards,
					imageInput = images.ToArray(),
					chats = backup.chats.ToArray(),
					userInfo = backup.userInfo,
					folder = parentFolder,
				};
				Backyard.Link.Image[] imageLinks; // Ignored
				GroupInstance groupInstance; // Ignored

				error = Backyard.Database.CreateNewParty(args, out groupInstance, out characterInstances, out imageLinks);
			}
			else // Legacy
			{
				if (cards.Length > 1) // Multi-character backup?
				{
					characterInstances = null;
					return WorkerError.Skipped; // Unsupported
				}

				// Write character to database
				var args = new Backyard.CreateCharacterArguments() {
					card = cards[0],
					imageInput = images.ToArray(),
					chats = backup.chats.ToArray(),
					userInfo = backup.userInfo,
					folder = parentFolder,
				};

				Backyard.Link.Image[] imageLinks; // Ignored
				characterInstances = new CharacterInstance[1];
				error = Backyard.Database.CreateNewCharacter(args, out characterInstances[0], out imageLinks);
			}

			if (error == Backyard.Error.UnsupportedFeature)
				return WorkerError.Skipped;
			if (error != Backyard.Error.NoError)
				return WorkerError.DatabaseError;
			return WorkerError.NoError;
		}
	}
}
