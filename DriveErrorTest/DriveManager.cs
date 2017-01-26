using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DriveErrorTest
{
	class DriveManager
	{
		private TestStartQueue _startQueue;

		public ObservableCollection<DriveInfoStorage> DriveList { get; set; }

		public DirectoryInfo SourceDirectory { get; set; }

		public void Initialize()
		{
			DriveList = new ObservableCollection<DriveInfoStorage>();
			PopulateDriveList();

			InitializeStartQueue();
		}

		private void PopulateDriveList()
		{
			DriveList.Clear();

			var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);

			foreach (var drive in drives.Where(drive => drive.DriveType == DriveType.Removable))
				DriveList.Add(new DriveInfoStorage(drive));
		}

		private void InitializeStartQueue()
		{
			_startQueue = new TestStartQueue();
			_startQueue.Initialize(1800000);
		}

		public void StartTest(int index)
		{
			_startQueue.Add(DriveList[index]);
		}

		public void PauseTest(int index)
		{
			DriveList[index].PauseTest();
		}

		public void StopTest(int index, bool force = false)
		{
			DriveList[index].StopTest(force);
		}

		public void StopAllTests(bool force = false)
		{
			foreach (var drive in DriveList)
				drive.StopTest(force);
		}

		public void ShowLogSelected(int selectedIndex)
		{
			var path = DriveList[selectedIndex].Settings.Log.Path;

			if (File.Exists(path))
				Process.Start(path);
		}
	}
}
