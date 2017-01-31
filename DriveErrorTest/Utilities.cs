using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Permissions;

namespace DriveErrorTest
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum TestingStatus
	{
		[Description("Неактивен")]
		NotActive,
		[Description("Готов")]
		StandingBy,
		[Description("В очереди")]
		Pending,
		[Description("Запускается")]
		Launched,
		[Description("Форматируется")]
		Formatting,
		[Description("Идёт запись")]
		Writing,
		[Description("Ошибок нет")]
		NoErrorsFound,
		[Description("Есть ошибки")]
		ErrorsFound,
		[Description("Ошибка!")]
		Fatal,
		[Description("Пауза")]
		Paused,
		[Description("Остановлен")]
		Stopped
	}

	public static class Utilities
	{
		public static bool FormatDriveWithCmd(string driveName, string volumeLabel, bool useQuickFormat = true)
		{
			if (driveName.Length != 2 || driveName[1] != ':' || !char.IsLetter(driveName[0]))
				return false;

			//query and format given drive         
			var searcher = new ManagementObjectSearcher
				(@"select * from Win32_Volume WHERE DriveLetter = '" + driveName + "'");
			foreach (ManagementObject vi in searcher.Get())
			{
				vi.InvokeMethod("Format", new object[]
				{"NTFS", useQuickFormat, 8192, volumeLabel, false});
			}

			return true;
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
