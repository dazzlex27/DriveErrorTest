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


		private void StartTest()
		{
			//try
			//{
			//	Title = GlobalContext.AppTitleTextBase + " - " + Drives[CbDrives.SelectedIndex].Name + Drives[CbDrives.SelectedIndex].VolumeLabel;
			//	_testingThread = new Thread(() => CreateTester(GetSelectedIndex(CbTimePeriod), GetCheckBoxValue(CbCleanStart) == true));
			//	_testingThread.Start();
			//	SetGuiAccess(false);
			//	SetStartStopButtonLabel(false);
			//	SetTestingStatusText("запущено");
			//	BtPausehTesting.Visibility = Visibility.Visible;
			//	SetGuiAccess(false);
			//}
			//catch (Exception)
			//{
			//	MessageBox.Show(
			//		"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства и файла журнала",
			//		"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			//}
		}

		private void StopTest()
		{
			//UnsubscribeFromTesterEvents();
			//_tester.StopTest();
			//do { } while (_tester.IsRunning);
			//SetStartStopButtonLabel(true);
			//SetTestingStatusText("остановлено");
			//SetBackgroundColor(Color.FromRgb(255, 255, 255));
			//SetTaskbarStatus(TaskbarItemProgressState.None, 0);
			//BtPausehTesting.Visibility = Visibility.Hidden;
			//SetCurrentFileText(" ");
			//SetGuiAccess(true);
		}
	}
}
