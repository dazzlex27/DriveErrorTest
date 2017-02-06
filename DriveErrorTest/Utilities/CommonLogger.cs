using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DriveErrorTest
{
	public static class CommonLogger
	{
		private static string _loggingPath;
		private static bool _isCreated;

		public static void LogWarning(string message)
		{
			if (!_isCreated)
				return;

			var file = new StreamWriter(Path.Combine(_loggingPath, "Warning.txt"), true);
			file.WriteLine("WARNING: " + DateTime.Now);
			file.WriteLine(message);
			file.WriteLine();
			file.Close();
		}

		public static void LogError(string message)
		{
			if (!_isCreated)
				return;

			var file = new StreamWriter(Path.Combine(_loggingPath, "Error.txt"), true);
			file.WriteLine("ERROR: " + DateTime.Now);
			file.WriteLine(message);
			file.WriteLine();
			file.Close();
		}

		public static void LogException(string message)
		{
			if (!_isCreated)
				return;

			var file = new StreamWriter(Path.Combine(_loggingPath, "Exception.txt"), true);
			file.WriteLine("EXCEPTION: " + DateTime.Now);
			file.WriteLine(message);
			file.WriteLine();
			file.Close();
		}

		public static void Initialize()
		{
			if (_isCreated)
				return;
			var securityIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			_loggingPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"FlashDriveTest",
				DateTime.Now.ToString("dd.MM.yyyy hh.mm.ss"));

			var directoryInfo = Directory.CreateDirectory(_loggingPath);
			bool modified;
			var directorySecurity = directoryInfo.GetAccessControl();
			var rule = new FileSystemAccessRule(
				securityIdentifier,
				FileSystemRights.Write |
				FileSystemRights.ReadAndExecute |
				FileSystemRights.Modify,
				InheritanceFlags.ContainerInherit |
				InheritanceFlags.ObjectInherit,
				PropagationFlags.InheritOnly,
				AccessControlType.Allow);
			directorySecurity.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
			directoryInfo.SetAccessControl(directorySecurity);

			_isCreated = true;
		}

		public static void CreateDriveLogFiles(ObservableCollection<DriveInfoStorage> driveList)
		{
			foreach (var drive in driveList)
			{
				try
				{
					var folderName = drive.Name.Replace(":\\", "_");
					var directory = Path.Combine(_loggingPath, "Drive logs", folderName);
					Directory.CreateDirectory(directory);
					drive.Settings.Log = new Logger(Path.Combine(directory, folderName + ".txt"));
					File.Create(drive.Settings.Log.Path);
				}
				catch (Exception ex)
				{
					LogException($"Failed to create folder for drive {drive.Name}; exception text: {ex}");
				}
			}
		}
	}
}
