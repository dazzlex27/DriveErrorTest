using System;
using System.IO;
using System.Threading;

namespace DriveErrorTest.Tester
{
	public class DriveTester
	{
		public event Action FormattingStarted;
		public event Action WritingStarted;
		public event Action<int> OnErrorCountChanged;
		public event Action<int> OnReadCyclesCountChanged;
		public event Action<int> OnWriteCyclesCountChanged;
		public event Action ErrorCountExceeded;
		private readonly DriveTesterSettings _settings;
		private readonly DriveTesterLogic _logic;
		private DateTime _lastUpdateTime;
		private int _readCyclesCount;
		private int _writeCyclesCount;
		private int _errorsCount;

		public bool IsRunning { get; private set; }

		public bool IsPaused { get; private set; }

		public int ErrorsCount
		{
			get { return _errorsCount; }
			private set
			{
				_errorsCount = value;
				OnErrorCountChanged?.Invoke(_errorsCount);
				if (_errorsCount == 100)
					BreakTestOnEmergency();
			}
		}

		public int ReadCyclesCount
		{
			get { return _readCyclesCount; }
			private set
			{
				_readCyclesCount = value;
				OnReadCyclesCountChanged?.Invoke(_readCyclesCount);
			}
		}

		public int WriteCyclesCount
		{
			get { return _writeCyclesCount; }
			private set
			{
				_writeCyclesCount = value;
				OnWriteCyclesCountChanged?.Invoke(_writeCyclesCount);
			}
		}

		public DriveTester(DriveInfo drive, DriveTesterSettings settings)
		{
			_settings = settings;
			_logic = new DriveTesterLogic(drive, settings);
			_logic.ErrorOccured += () => { ++ErrorsCount; };
			_logic.FormattingStarted += () => { FormattingStarted?.Invoke(); };
			_logic.WritingStarted += () => { WritingStarted?.Invoke(); };
			_logic.ReadCycleCompleted += () => { ++ReadCyclesCount; };
			_logic.WriteCycleCompleted += () =>
			{
				++WriteCyclesCount;
				_lastUpdateTime = DateTime.Now;
				_settings.Log.LogInfo(_lastUpdateTime,
					"Выполнена плановая перезапись данных, проведено циклов записи - " + _writeCyclesCount);
			};

			ErrorsCount = 0;
		}

		public void RunTest()
		{
			IsRunning = true;
			_settings.Log.LogInfo(DateTime.Now, "Тестирование запущено");

			try
			{
				if (_settings.CleanStart)
				{
					do
					{
					} while (!_logic.LoadFilesToDrive() && IsRunning);
				}
				else
					_logic.GetFilesFromSourceDirectory();

				ErrorsCount = 0;

				while (IsRunning)
				{
					if (IsPaused)
					{
						Thread.Sleep(1000);
						continue;
					}

					if (DateTime.Now - _lastUpdateTime > _settings.RewritePeriod)
					{
						_settings.Log.LogInfo(DateTime.Now, "Цикл чтения окончен. Всего итераций чтения - " + _readCyclesCount);
						do
						{
						} while (!_logic.LoadFilesToDrive() && IsRunning);
					}

					_logic.RunCheckCycle();
				}
			}
			catch (DriveNotFoundException ex)
			{
				++ErrorsCount;
				_settings.Log.LogException(DateTime.Now, "Устройство не найдено", ex.ToString());
			}
			catch (Exception ex)
			{
				++ErrorsCount;
				_settings.Log.LogException(DateTime.Now, "Во время выполнения возникло исключение", ex.ToString());
			}

			_settings.Log.LogInfo(DateTime.Now, "Тестирование прервано");
		}

		public void ResumeTest()
		{
			if (!IsRunning || !IsPaused)
				return;

			IsPaused = false;
			_settings.Log.LogInfo(DateTime.Now, "Тестирование продолжено");
		}

		public void PauseTest()
		{
			if (!IsRunning)
				return;

			IsPaused = true;
			_settings.Log.LogInfo(DateTime.Now, "Тестирование поставлено на паузу");
		}

		public void StopTest()
		{
			if (IsRunning)
				return;

			IsRunning = false;
			_settings.Log.LogInfo(DateTime.Now, "Тестирование остановлено");
		}

		private void BreakTestOnEmergency()
		{
			StopTest();
			ErrorCountExceeded?.Invoke();
			_settings.Log.LogInfo(DateTime.Now, "Тестирование аварийно завершено");
		}
	}
}
