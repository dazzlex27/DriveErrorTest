using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;

namespace DriveErrorTest
{
	public static class Utilities
	{
		public static bool FormatDriveWithCmd(string driveName, string volumeLabel, bool useQuickFormat = true)
		{
			var command = "format" + driveName + " /y" + (useQuickFormat ? "/q" : "") + " /fs:NTFS";
			command += " && label " + driveName + " " + volumeLabel;

			var proc1 = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					Verb = "runas",
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					Arguments = command
				}
			};
			proc1.Start();
			proc1.WaitForExit();

			return proc1.ExitCode == 0;
		}

		public static IEnumerable<string> Traverse(string rootDirectory)
		{
			var files = Enumerable.Empty<string>();
			var directories = Enumerable.Empty<string>();
			try
			{
				// The test for UnauthorizedAccessException.
				var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
				permission.Demand();

				files = Directory.GetFiles(rootDirectory);
				directories = Directory.GetDirectories(rootDirectory);
			}
			catch
			{
				// Ignore folder (access denied).
				rootDirectory = null;
			}

			if (rootDirectory != null)
				yield return rootDirectory;

			foreach (var file in files)
			{
				yield return file;
			}

			// Recursive call for SelectMany.
			var subdirectoryItems = directories.SelectMany(Traverse);
			foreach (var result in subdirectoryItems)
			{
				yield return result;
			}
		}

		public static bool CompareTwoFiles(string filepath1, string filepath2)
		{
			using (var file = File.Open(filepath1, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var file2 = File.Open(filepath2, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					if (file.Length != file2.Length)
						return false;

					int count;
					const int size = 0x1000000;

					var buffer = new byte[size];
					var buffer2 = new byte[size];

					while ((count = file.Read(buffer, 0, buffer.Length)) > 0)
					{
						file2.Read(buffer2, 0, buffer2.Length);

						for (var i = 0; i < count; i++)
						{
							if (buffer[i] != buffer2[i])
								return false;
						}
					}
				}
			}

			return true;
		}

		public static List<string> GetFilesInDirectory(DirectoryInfo directory)
		{
			var result = new List<string>();
			var driveEnumeration = Traverse(directory.FullName);

			foreach (var item in driveEnumeration)
			{
				var attribute = File.GetAttributes(item);
				if (attribute.HasFlag(FileAttributes.Directory))
					continue;

				var actualFilename = item.Substring(directory.FullName.Length,
					item.Length - directory.FullName.Length);
				if (!actualFilename.Contains(directory + "\\System Volume Information\\"))
					result.Add(actualFilename);
			}

			return result;
		}
	}
}
