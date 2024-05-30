using System;
using System.Threading;

namespace Ginger
{
	public static class FileMutex
	{
		private static Mutex _fileMutex = null;

		private static string MakeMutexId(string filename)
		{
			return string.Format("Ginger_{0}", filename.ToLowerInvariant().Replace('\\', '/'));
		}

		public static bool Acquire(string filename)
		{
			if (string.IsNullOrEmpty(filename)
				|| string.Compare(filename, Current.Filename, StringComparison.OrdinalIgnoreCase) == 0) // Already open
				return true;

			Release();

			try
			{
				bool success;
				_fileMutex = new Mutex(true, MakeMutexId(filename), out success);
				if (success)
					return true;

				_fileMutex.Close();
				_fileMutex = null;
				return false;
			}
			catch
			{
				_fileMutex = null;
			}
			return false;
		}

		public static bool CanAcquire(string filename)
		{
			if (string.IsNullOrEmpty(filename)
				|| string.Compare(filename, Current.Filename, StringComparison.OrdinalIgnoreCase) == 0) // Already open
				return true;

			try
			{
				Mutex mutex = null;
				if (Mutex.TryOpenExisting(MakeMutexId(filename), out mutex))
				{
					// Exists
					if (mutex != null)
						mutex.Close();
					return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static void Release()
		{
			if (_fileMutex == null)
				return;

			try
			{
				_fileMutex.Close();
				_fileMutex = null;
			}
			catch
			{
				_fileMutex = null;
			}
		}
	}
}
