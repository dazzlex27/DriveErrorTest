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

		public void StartTest()
		{
			_testerThread = new Thread(() => LaunchTestingThread());
			_testerThread.Start();
		}

		private void LaunchTestingThread()
		{
			_tester = new DriveTester(_drive, Settings);
			_tester.RunTest();
		}

		public void PauseTest()
		{

		}

		public void StopTest(bool force)
		{
			if (_tester == null || !_tester.IsRunning)
				return;

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
