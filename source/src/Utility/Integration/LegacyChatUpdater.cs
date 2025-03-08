using System.Collections.Generic;
using System.Linq;

namespace Ginger.Integration
{
	using GroupInstance = Backyard.GroupInstance;
	using ChatParameters = Backyard.ChatParameters;

	using WorkerError = AsyncTaskQueue<Backyard.GroupInstance, RepairLegacyChatsWorker.WorkerResult>.Error;

	public class LegacyChatUpdater : AsyncTask<RepairLegacyChatsWorker, GroupInstance, RepairLegacyChatsWorker.WorkerResult>
	{
		public delegate void OnProgress(int percent);
		public event OnProgress onProgress;

		public delegate void OnResult(Result error);
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
			public int numCharacters;
			public int numChats;
		}

		protected override void Progress(int percent)
		{
			onProgress?.Invoke(percent);
		}

		protected override void Completed(AsyncResult result)
		{
			int numCharacters = result.results.Count(r => r.numModified > 0);
			int numChats = result.results.Sum(r => r.numModified);

			switch (result.error)
			{
			case WorkerError.NoError:
				onComplete?.Invoke(new Result() {
					error = Error.NoError,
					numCharacters = numCharacters,
					numChats = numChats,
				});
				break;
			case WorkerError.Cancelled:
				onComplete?.Invoke(new Result() {
					error = Error.Cancelled,
					numCharacters = numCharacters,
					numChats = numChats,
				});
				break;
			default:
				onComplete?.Invoke(new Result() {
					error = Error.UnknownError,
					numCharacters = numCharacters,
					numChats = numChats,
				});
				break;
			}
		}
	}

	public class RepairLegacyChatsWorker : AsyncWorkerBase<GroupInstance, RepairLegacyChatsWorker.WorkerResult>
	{
		public enum WorkerError
		{
			NoError = 0,
			UnknownError,
			DatabaseError,
		}

		public struct WorkerArguments
		{
			public GroupInstance group;
			public ChatParameters chatParameters;
		}

		public struct WorkerResult
		{
			public int numModified;
			public WorkerError error;
		}

		public override bool Execute(GroupInstance groupInstance, out WorkerResult result)
		{
			int modified;
			WorkerError error = RepairChat(groupInstance, out modified);
			result = new WorkerResult();

			if (error == WorkerError.NoError)
			{
				result.numModified += modified;
				return true;
			}

			result.error = error;
			return false;
		}

		private WorkerError RepairChat(GroupInstance group, out int modified)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				modified = 0;
				return WorkerError.DatabaseError;
			}

			var error = Backyard.Database.RepairChats(group.instanceId, out modified);
			if (error == Backyard.Error.SQLCommandFailed || error == Backyard.Error.NotConnected)
				return WorkerError.DatabaseError;
			else if (error != Backyard.Error.NoError)
				return WorkerError.UnknownError;

			return WorkerError.NoError;
		}
	}

}
