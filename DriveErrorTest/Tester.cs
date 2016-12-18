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
		private readonly DriveInfo _drive;
		private readonly DirectoryInfo _sourceDirectory;
		private readonly TimeSpan _updatePeriod;
		private DateTime _lastUpdateTime;
		private readonly MainWindow _parentWindow;
		private int _errorsNum;
		private readonly Dictionary<string, bool> _files = new Dictionary<string, bool>();
		private readonly FileInfo _logFile;
		private int _writeCycles;

		public bool IsRunning { get; private set; }

		public Tester(MainWindow parentWindow, DriveInfo drive, string dataPath, string logPath, TimeSpan updatePeriod)
		{
			_parentWindow = parentWindow;
			_drive = drive;
			_sourceDirectory = new DirectoryInfo(dataPath);
			_logFile = new FileInfo(logPath);
			_updatePeriod = updatePeriod;
			_errorsNum = 0;
		}

		public void RunTest()
		{
			IsRunning = true;
			Utilities.LogEvent(_logFile, DateTime.Now, "Тестирование запущено");
			
			_parentWindow.SetBackgroundColor(Color.FromRgb(255, 255, 255));
			do { } while (!LoadFilesToDrive());

			while (IsRunning)
			{
				if (DateTime.Now - _lastUpdateTime > _updatePeriod)
					do { } while (!LoadFilesToDrive());

				RunCheckCycle();
				SetErrorStatus();
			}

			_parentWindow.SetCurrentFile("");
			_parentWindow.SetBackgroundColor(Color.FromRgb(255, 255, 255));
			_parentWindow.SetTaskbarStatus(TaskbarItemProgressState.None, 0);
			Utilities.LogEvent(_logFile, DateTime.Now, "Тестирование прервано");
			_parentWindow.SetTestingStatusText("остановлено");
		}

		public void StopTest()
		{
			IsRunning = false;
		}

		private void RunCheckCycle()
		{
			var filesFromDrive = Utilities.GetFilesOnDrive(_drive);

			if (filesFromDrive == null)
			{
				_errorsNum++;
				Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось построить список файлов на устройстве");
				return;
			}
			
			if (filesFromDrive.Count != _files.Count)
			{
				_errorsNum++;
				Utilities.LogEvent(_logFile,
					DateTime.Now,
					"Ошибка! Количество файлов не совпадает. Ожидалось - " + _files.Count + ", посчитано - " + filesFromDrive.Count);
			}

			try
			{
				foreach (var file in _files.Where(file => file.Value))
				{
					_parentWindow.SetCurrentFile(file.Key);
					var index = filesFromDrive.IndexOf(file.Key);
					if (index > -1)
					{
						bool identical;

						try
						{
							identical = Utilities.CompareTwoFiles(Path.Combine(_sourceDirectory.FullName, file.Key),
								Path.Combine(_drive.RootDirectory.FullName, filesFromDrive[index]));
						}
						catch (Exception)
						{
							_errorsNum++;
							_files[file.Key] = false;
							Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось сравнить версии файла " + file.Key);
							continue;
						}

						if (identical)
							continue;

						_errorsNum++;
						_files[file.Key] = false;
						Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Файл " + file.Key + " не совпадает с исходным");
						SetErrorStatus();
					}
					else
					{
						_errorsNum++;
						_files[file.Key] = false;
						Utilities.LogEvent(_logFile, DateTime.Now,  "Ошибка! Файл " + file.Key + " не найден");
						SetErrorStatus();
					}

					Thread.Sleep(10);
				}

				_parentWindow.SetCurrentFile("");
			}
			catch (Exception)
			{
			}
		}

		private bool LoadFilesToDrive()
		{
			_parentWindow.SetTestingStatusText("идет форматирование...");
			if (!FormatDrive())
			{
				++_errorsNum;
				Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось форматировать устройство");
				SetErrorStatus();
				return false;
			}
			_parentWindow.SetTestingStatusText("идет запись данных...");
			if (!WriteFilesToDrive())
			{
				++_errorsNum;
				Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось записать данные на устройство");
				SetErrorStatus();
				return false;
			}
			_lastUpdateTime = DateTime.Now;
			Utilities.LogEvent(_logFile, _lastUpdateTime, "Выполнена плановая перезапись данных, проведено циклов записи - " + ++_writeCycles);
			_parentWindow.SetWryteCycles(_writeCycles);
			SetErrorStatus();

			return true;
		}

		private void SetErrorStatus()
		{
			if (_errorsNum == 0)
			{
				_parentWindow.SetTestingStatusText("ошибок не найдено");
				_parentWindow.SetBackgroundColor(Color.FromRgb(191, 235, 171));
				_parentWindow.SetTaskbarStatus(TaskbarItemProgressState.Normal, 1);
			}
			else
			{
				if (_errorsNum >= 100)
				{
					_parentWindow.BreakTestOnEmergency();
					return;
				}

				_parentWindow.SetTestingStatusText("обнаружено " + _errorsNum + " ошибок");
				_parentWindow.SetBackgroundColor(Color.FromRgb(245, 105, 105));
				_parentWindow.SetTaskbarStatus(TaskbarItemProgressState.Error, 1);
			}
		}

		private bool FormatDrive()
		{
			try
			{
				return Utilities.FormatDriveWithCmd(_drive.Name.Substring(0,2), _drive.VolumeLabel);
			}
			catch (Exception)
			{
				_errorsNum++;
				Utilities.LogEvent(_logFile, DateTime.Now,"Ошибка! Не удалось форматировать устройство");
				return false;
			}
		}

		private bool WriteFilesToDrive()
		{
			try
			{
				_files.Clear();
				// Create all of the directories
				foreach (var dirPath in Directory.GetDirectories(_sourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
					Directory.CreateDirectory(dirPath.Replace(_sourceDirectory.FullName, _drive.RootDirectory.FullName));

				// Copy all the files & Replaces any files with the same name
				foreach (var path in Directory.GetFiles(_sourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
				{
					var actualFilename = path.Substring(_sourceDirectory.FullName.Length + 1,
						path.Length - _sourceDirectory.FullName.Length - 1);
					_files.Add(actualFilename, true);
					File.Copy(path, path.Replace(_sourceDirectory.FullName, _drive.RootDirectory.FullName), true);
				}

				return true;
			}
			catch (Exception)
			{
				_errorsNum++;
				Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось записать файлы на диск");
				return false;
			}
		}
	}
}
