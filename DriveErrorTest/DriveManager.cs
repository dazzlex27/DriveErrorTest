using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DriveErrorTest
{
	class DriveManager
	{
		public ObservableCollection<DriveInfoStorage> DriveList { get; set; }

		public DirectoryInfo SourceDirectory { get; set; }

		public void Initialize()
		{
			DriveList = new ObservableCollection<DriveInfoStorage>();
			PopulateDriveList();
		}

		private void PopulateDriveList()
		{
			DriveList.Clear();

			var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);

			foreach (var drive in drives.Where(drive => drive.DriveType == DriveType.Removable))
				DriveList.Add(new DriveInfoStorage(drive));
		}

		public void StartTest(int index, bool cleanStart = false)
		{
			DriveList[index].StartTest(cleanStart);
		}

		public void StartTest(DriveInfoStorage drive, bool cleanStart = false)
		{
			DriveList[DriveList.IndexOf(drive)].StartTest(cleanStart);
		}

		public void StopAllTests()
		{
			foreach (var drive in DriveList)
				drive.StopTest(false);
		}

		public void ShowLogSelected(int selectedIndex)
		{
			var path = DriveList[selectedIndex].Settings.Log.Path;

			if (File.Exists(path))
				Process.Start(path);
		}
	}
}
