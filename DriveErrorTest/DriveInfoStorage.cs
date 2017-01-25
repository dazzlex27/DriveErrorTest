using System.IO;
using System.Threading;

namespace DriveErrorTest
{
	public enum TestingStatus
	{
		NotActive,
		Pending,
		NoErrorsFound,
		ErrorsFound,
		Fatal,
		Paused
	}

	public class DriveInfoStorage
	{
		private readonly DriveInfo _drive;
		private DriveTester _tester;
		private Thread _testerThread;

		public DriveTesterSettings Settings { get; set; }

		public DriveInfoStorage(DriveInfo drive)
		{
			_drive = drive;
		}

		public string Name { get; set; }

		public TestingStatus HealthStatus { get; set; }

		public DriveInfo GetDeviceInfo()
		{
			return _drive;
		}

		public void StartTest(bool cleanStart)
		{
			_testerThread = new Thread(() => LaunchTestingThread(cleanStart));
			_testerThread.Start();
		}

		private void LaunchTestingThread(bool cleanStart)
		{
			_tester = new DriveTester(_drive, Settings)
			{
				CleanStart = cleanStart
			};

			_tester.RunTest();
		}

		public void StopTest(bool force)
		{
			if (force)
			{
				try { _testerThread.Abort(); }
				catch { }
			}
			else
			{
				_tester.StopTest();
				do { } while (_tester.IsRunning);
			}
		}
	}
}
