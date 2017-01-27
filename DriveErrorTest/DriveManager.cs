using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DriveErrorTest
{
	class DriveManager
	{
		private TestStartQueue _startQueue;
		private DirectoryInfo _sourceDirectory;

		public ObservableCollection<DriveInfoStorage> DriveList { get; set; }

		public DirectoryInfo SourceDirectory
		{
			get { return _sourceDirectory; }
			set
			{
				_sourceDirectory = value;
				foreach (var drive in DriveList)
					drive.Settings.SourceDirectory = _sourceDirectory;
			}
		}

		public bool TestsRunning { get; private set; }

		public void Initialize()
		{
			DriveList = new ObservableCollection<DriveInfoStorage>();
			PopulateDriveList();

			CommonLogger.CreateDriveLogFiles(DriveList);

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
			_startQueue.Initialize(1200000);
		}

		public void StartTest(object item)
		{
			var temp = item as DriveInfoStorage;
			if (temp.HealthStatus != TestingStatus.Paused)
			{
				_startQueue.Add(DriveList[DriveList.IndexOf(temp)]);
				DriveList[DriveList.IndexOf(temp)].SetHealthStatus(TestingStatus.Pending);
				TestsRunning = true;
			}
			else
				temp.ResumeTest();
		}

		public void PauseTest(object item)
		{
			var temp = item as DriveInfoStorage;
			DriveList[DriveList.IndexOf(temp)].PauseTest();
		}

		public void StopTest(object item, bool force = false)
		{
			var temp = item as DriveInfoStorage;
			DriveList[DriveList.IndexOf(temp)].StopTest(force);

			foreach (var drive in DriveList)
			{
				if (drive.Running)
					break;
			}
		}

		public void StopAllTests(bool force = false)
		{
			foreach (var drive in DriveList)
			{
				drive.StopTest(force);
				TestsRunning = false;
			}
		}

		public void ShowLogSelected(object item)
		{
			var temp = item as DriveInfoStorage;
			var path = DriveList[DriveList.IndexOf(temp)].Settings.Log.Path;

			if (File.Exists(path))
				Process.Start(path);
		}
	}
}
