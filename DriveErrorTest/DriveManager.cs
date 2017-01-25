using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DriveErrorTest
{
	class DriveManager
	{
		public ObservableCollection<DriveInfoStorage> DriveList { get; set; }

		public void Initialize()
		{
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
	}
}
