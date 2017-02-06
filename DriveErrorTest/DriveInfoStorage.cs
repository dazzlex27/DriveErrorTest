using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using DriveErrorTest.Tester;

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
		private uint _restartsLeft;

		public event Action CriticalErrorOccured;
		public event PropertyChangedEventHandler PropertyChanged;

		public DriveTesterSettings Settings { get; set; }

		public bool Running { get; private set; }

		[DisplayName("Наименование")]
		public string Name
		{
			get { return _name; }
			private set
			{
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
			}
		}

		[DisplayName("Состояние")]
		public TestingStatus HealthStatus
		{
			get { return _healthStatus; }
			private set
			{
				_healthStatus = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HealthStatus"));
			}
		}

		[DisplayName("Циклов записи")]
		public int WriteCycles
		{
			get { return _writeCycles; }
			private set
			{
				_writeCycles = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WriteCycles"));
			}
		}

		[DisplayName("Циклов чтения")]
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
			_restartsLeft = Settings.RecoveryAttempts;
		}

		public uint GetRestartsLeftAndDecrement()
		{
			return _restartsLeft--;
		}

		public DriveInfo GetDeviceInfo()
		{
			return _drive;
		}

		public void StartTest()
		{
			if (Running)
				return;

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
			_tester.ErrorCountExceeded += () => 
			{
				HealthStatus = TestingStatus.Fatal;
				CriticalErrorOccured?.Invoke();
			};

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
