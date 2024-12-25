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
		}

		protected override void Progress(int percent)
		{
			onProgress?.Invoke(percent);
		}

		protected override void Completed(AsyncResult result)
		{
			var filenames = result.results
				.OrderBy(e => e.modifiedDate)
				.Select(e => e.filename)
				.ToArray();

			if (result.error == AsyncTaskQueue<string, AsyncFileTypeCheckerWorker.WorkerResult>.Error.NoError)
				onComplete?.Invoke(new Result() {
					error = Error.NoError,
					filenames = filenames,
				});
			else if (result.error == AsyncTaskQueue<string, AsyncFileTypeCheckerWorker.WorkerResult>.Error.Cancelled)
				onComplete?.Invoke(new Result() {
					error = Error.Cancelled,
					filenames = filenames,
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
		}

		public override bool Execute(string filename, out WorkerResult result)
		{
			if (FileUtil.CheckFileType(filename) != FileUtil.FileType.Unknown)
			{
				DateTime modifiedDate;
				try
				{
					modifiedDate = new FileInfo(filename).LastWriteTime;
					result = new WorkerResult() {
						filename = filename,
						modifiedDate = modifiedDate,
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
