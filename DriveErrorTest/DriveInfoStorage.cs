using System.ComponentModel;
using System.IO;
using System.Threading;

namespace DriveErrorTest
{
	public class DriveInfoStorage : INotifyPropertyChanged
	{
		private readonly DriveInfo _drive;
		private DriveTester _tester;
		private Thread _testerThread;
		private string _name;
		private TestingStatus _healthStatus;
		private int _writeCycles;
		private int _readCycles;

		public event PropertyChangedEventHandler PropertyChanged;

		public DriveTesterSettings Settings { get; set; }

		public bool Running { get; private set; }

		public string Name
		{
			get { return _name; }
			private set
			{
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
			}
		}

		public TestingStatus HealthStatus
		{
			get { return _healthStatus; }
			private set
			{
				_healthStatus = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HealthStatus"));
			}
		}

		public int WriteCycles
		{
			get { return _writeCycles; }
			private set
			{
				_writeCycles = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WriteCycles"));
			}
		}

		public int ReadCycles
		{
			get { return _readCycles; }
			private set
			{
				_readCycles = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ReadCycles"));
			}
		}

		public DriveInfoStorage(DriveInfo drive)
		{
			_drive = drive;
			Name = drive.Name + drive.VolumeLabel;
			Settings = new DriveTesterSettings();
		}

		public DriveInfo GetDeviceInfo()
		{
			return _drive;
		}

		public void StartTest()
		{
			_testerThread = new Thread(LaunchTestingThread);
			_testerThread.Start();
			HealthStatus = TestingStatus.Launched;
			Running = true;
		}

		public void SetHealthStatus(TestingStatus status)
		{
			HealthStatus = status;
		}

		public void ResumeTest()
		{
			if (_tester != null && _tester.IsPaused)
			{
				_tester.ResumeTest();
				HealthStatus = _tester.ErrorsCount == 0 ? TestingStatus.NoErrorsFound : TestingStatus.ErrorsFound;
			}
		}

		private void LaunchTestingThread()
		{
			_tester = new DriveTester(_drive, Settings);

			_tester.FormattingStarted += () => { HealthStatus = TestingStatus.Formatting; };
			_tester.WritingStarted += () => { HealthStatus = TestingStatus.Writing; };
			_tester.OnReadCyclesCountChanged += value => { ReadCycles = value; };
			_tester.OnWriteCyclesCountChanged += value => { WriteCycles = value; };
			_tester.OnErrorCountChanged += value => { HealthStatus = value == 0 ? TestingStatus.NoErrorsFound : TestingStatus.ErrorsFound; };

			_tester.RunTest();
		}

		public void PauseTest()
		{
			_tester.PauseTest();
			HealthStatus = TestingStatus.Paused;
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

			Running = false;
			HealthStatus = TestingStatus.Stopped;
		}
	}
}
