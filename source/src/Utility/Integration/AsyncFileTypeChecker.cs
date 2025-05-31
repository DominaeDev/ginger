using System;
using System.IO;
using System.Linq;

namespace Ginger
{
	public class AsyncFileTypeChecker : AsyncTask<AsyncFileTypeCheckerWorker, string, AsyncFileTypeCheckerWorker.WorkerResult>
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result result);
		public event OnResult onComplete;

		public enum Error
		{
			NoError,
			Cancelled,
			UnknownError,
		}

		public struct Result
		{
			public Error error;
			public string[] filenames;
			public int characters;
			public int groups;
		}

		protected override void Progress(int percent)
		{
			onProgress?.Invoke(percent);
		}

		protected override void Completed(AsyncResult result)
		{
			var filenames = result.results
				.OrderBy(r => r.modifiedDate)
				.Select(r => r.filename)
				.ToArray();
			int characters = result.results.Sum(r => r.characters);
			int groups = result.results.Count(r => r.characters > 1);

			if (result.error == AsyncTaskQueue<string, AsyncFileTypeCheckerWorker.WorkerResult>.Error.NoError)
				onComplete?.Invoke(new Result() {
					error = Error.NoError,
					filenames = filenames,
					characters = characters,
					groups = groups,
				});
			else if (result.error == AsyncTaskQueue<string, AsyncFileTypeCheckerWorker.WorkerResult>.Error.Cancelled)
				onComplete?.Invoke(new Result() {
					error = Error.Cancelled,
					filenames = filenames,
					characters = characters,
					groups = groups,
				});
			else
				onComplete?.Invoke(new Result() {
					error = Error.UnknownError,
					filenames = filenames,
				});
		}
	}

	public class AsyncFileTypeCheckerWorker : AsyncWorkerBase<string, AsyncFileTypeCheckerWorker.WorkerResult>
	{
		public struct WorkerResult
		{
			public string filename;
			public DateTime modifiedDate;
			public int characters;
		}

		public override bool Execute(string filename, out WorkerResult result)
		{
			int count;
			var fileType = FileUtil.CheckFileType(filename, out count);
			if (fileType != FileUtil.FileType.Unknown)
			{
				DateTime modifiedDate;
				try
				{
					modifiedDate = new FileInfo(filename).LastWriteTime;
					result = new WorkerResult() {
						filename = filename,
						modifiedDate = modifiedDate,
						characters = count,
					};
					return true;
				}
				catch
				{
				}
			}
			result = default(WorkerResult);
			return false;
		}
	}
}
