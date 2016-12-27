using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shell;
using System.Threading;

namespace DriveErrorTest
{
	internal class Tester
	{
		public event Action<int> OnErrorCountChanged;
		public event Action<ulong> OnReadCyclesCountChanged;
		public event Action<ulong> OnWriteCyclesCountChanged;
		public event Action<string> OnTestingStatusChanged;
		public event Action<string> OnCurrentFileChanged;
		private readonly DriveInfo _drive;
		private readonly DirectoryInfo _sourceDirectory;
		private readonly TimeSpan _updatePeriod;
		private DateTime _lastUpdateTime;
		private readonly Dictionary<string, bool> _files;// = new Dictionary<string, bool>();
		private readonly Logger _logger;
		private ulong _readCyclesCount;
		private int _writeCyclesCount;
		private int _errorsCount;

		public bool IsRunning { get; private set; }

		public bool CleanStart { get; set; }

		public int ErrorsCount
		{
			get { return _errorsCount; }
			private set
			{
				_errorsCount = value;
				OnErrorCountChanged?.Invoke(++_errorsCount);
				if (_errorsCount == 100)
					BreakTestOnEmergency();
			}
		}

		public ulong ReadCyclesCount
		{
			get { return _readCyclesCount; }
			private set
			{
				_readCyclesCount = value;
				OnReadCyclesCountChanged?.Invoke(++_readCyclesCount);
			}
		}

		public int WriteCyclesCount
		{
			get { return _writeCyclesCount; }
			private set
			{
				_writeCyclesCount = value;
				OnWriteCyclesCountChanged?.Invoke(++_readCyclesCount);
			}
		}

		private int IncrementErrorCount()
		{
			return ++ErrorsCount;
		}

		private ulong IncrementReadCycles()
		{
			return ++ReadCyclesCount;
		}

		private int IncrementWriteCycles()
		{
			return ++WriteCyclesCount;
		}

		private void SetCurrentFile(string currentFile)
		{
			OnCurrentFileChanged?.Invoke(currentFile);
		}

		private void SetTestingStatus(string status)
		{
			OnTestingStatusChanged?.Invoke(status);
		}

		public Tester(Logger logger, DriveInfo drive, string dataPath, TimeSpan updatePeriod)
		{
			_drive = drive;
			_sourceDirectory = new DirectoryInfo(dataPath);
			_files = new Dictionary<string, bool>();
			_logger = logger;

			_updatePeriod = updatePeriod;
			ErrorsCount = 0;
		}

		public void RunTest()
		{
			IsRunning = true;
			_logger.LogInfo(DateTime.Now, "Тестирование запущено");

			if (CleanStart)
			{
				do { } while (!LoadFilesToDrive() && IsRunning);
			}
			else
			{
				GetFilesFromSourceDirectory();
				_lastUpdateTime = DateTime.Now;
			}

			while (IsRunning)
			{
				if (DateTime.Now - _lastUpdateTime > _updatePeriod)
				{
					_logger.LogInfo(DateTime.Now, "Цикл чтения окончен. Всего итераций чтения - " + ReadCyclesCount);
					do { } while (!LoadFilesToDrive() && IsRunning);
				}

				RunCheckCycle();
			}

			SetCurrentFile("");
			_logger.LogInfo(DateTime.Now, "Тестирование прервано");
			SetTestingStatus("остановлено");
		}

		public void StopTest()
		{
			IsRunning = false;
		}

		private void BreakTestOnEmergency()
		{
			StopTest();
			_logger.LogInfo(DateTime.Now, "Тестирование аварийно завершено. Число ошибок превысило 100");
		}

		private void RunCheckCycle()
		{
			List<string> filesFromDrive;

			try
			{
				filesFromDrive = Utilities.GetFilesInDirectory(_drive.RootDirectory);
			}
			catch (Exception ex)
			{
				IncrementErrorCount();
				_logger.LogException(DateTime.Now, "Не удалось построить список файлов на устройстве", ex.ToString());
				return;
			}
			
			if (filesFromDrive.Count != _files.Count)
			{
				IncrementErrorCount();
				_logger.LogError(DateTime.Now,
					"Количество файлов не совпадает. Ожидалось - " + _files.Count + ", посчитано - " + filesFromDrive.Count);
			}

			try
			{
				CompareAllFiles(filesFromDrive);
			}
			catch (Exception ex)
			{
				IncrementErrorCount();
				_logger.LogException(DateTime.Now, "Не удалось выполнить чтение с устройства", ex.ToString());
			}
			finally
			{

				SetCurrentFile("");

				Thread.Sleep(10);
			}
		}

		private void CompareAllFiles(IList<string> filesFromDrive)
		{
			foreach (var file in _files.Where(file => file.Value))
			{
				SetCurrentFile(file.Key);
				var index = filesFromDrive.IndexOf(file.Key);
				if (index > -1)
				{
					bool identical;

					try
					{
						identical = Utilities.CompareTwoFiles(Path.Combine(_sourceDirectory.FullName, file.Key),
							Path.Combine(_drive.RootDirectory.FullName, filesFromDrive[index]));
					}
					catch (Exception ex)
					{
						IncrementErrorCount();
						_files[file.Key] = false;
						_logger.LogException(DateTime.Now, "Не удалось сравнить версии файла " + file.Key, ex.ToString());
						continue;
					}
					finally
					{
						IncrementReadCycles();
					}

					if (identical)
						continue;

					IncrementErrorCount();
					_files[file.Key] = false;
					_logger.LogError(DateTime.Now, "Файл " + file.Key + " не совпадает с исходным");
				}
				else
				{
					IncrementErrorCount();
					_files[file.Key] = false;
					_logger.LogError(DateTime.Now, "Файл " + file.Key + " не найден");
				}
			}
		}

		private bool LoadFilesToDrive()
		{
			SetTestingStatus("форматирование...");
			if (!FormatDrive())
			{
				IncrementErrorCount();
				return false;
			}

			SetTestingStatus("получение списка файлов...");
			if (!GetFilesFromSourceDirectory())
			{
				IncrementErrorCount();
				return false;
			}

			SetTestingStatus("запись данных...");
			if (!WriteFilesToDrive())
			{
				IncrementErrorCount();
				return false;
			}

			_lastUpdateTime = DateTime.Now;
			_logger.LogInfo(_lastUpdateTime,
				"Выполнена плановая перезапись данных, проведено циклов записи - " + IncrementWriteCycles());

			return true;
		}

		private bool FormatDrive()
		{
			try
			{
				return Utilities.FormatDriveWithCmd(_drive.Name.Substring(0, 2), _drive.VolumeLabel);
			}
			catch (Exception ex)
			{
				_logger.LogException(DateTime.Now, "Не удалось форматировать устройство", ex.ToString());
				return false;
			}
		}

		private bool GetFilesFromSourceDirectory()
		{
			try
			{
				_files.Clear();
				var filesFromSourceDirectory = Utilities.GetFilesInDirectory(_sourceDirectory);
				foreach (var path in filesFromSourceDirectory)
				{
					var actualFilename = path.Substring(1, path.Length - 1);
					_files.Add(actualFilename, true);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException(DateTime.Now, "Не удалось получить список файлов в источнике", ex.ToString());
				return false;
			}
		}

		private bool WriteFilesToDrive()
		{
			try
			{
				// Create all of the directories
				foreach (var dirPath in Directory.GetDirectories(_sourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
					Directory.CreateDirectory(dirPath.Replace(_sourceDirectory.FullName, _drive.RootDirectory.FullName));

				// Copy all the files & Replaces any files with the same name
				foreach (var path in Directory.GetFiles(_sourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
				{
					File.Copy(path, path.Replace(_sourceDirectory.FullName, _drive.RootDirectory.FullName), true);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException(DateTime.Now, "Ошибка! Не удалось записать файлы на диск", ex.ToString());
				return false;
			}
		}
	}
}
