using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DriveErrorTest
{
	internal class Tester
	{
		private bool _running; 
		private readonly DriveInfo _drive;
		private readonly DirectoryInfo _sourceDirectory;
		private readonly TimeSpan _updatePeriod;
		private DateTime _lastUpdateTime;
		private readonly MainWindow _parentWindow;
		private int _errorsNum;
		private readonly Dictionary<string, bool> _files = new Dictionary<string, bool>();
		private readonly FileInfo _logFile;

		public Tester(MainWindow parentWindow, DriveInfo drive, string dataPath, string logPath, TimeSpan updatePeriod)
		{
			_parentWindow = parentWindow;
			_drive = drive;
			_sourceDirectory = new DirectoryInfo(dataPath);
			_logFile = new FileInfo(logPath);
			_updatePeriod = updatePeriod;
			_errorsNum = 0;
		}

		public void StartTest()
		{
			_running = true;
			Utilities.LogEvent(_logFile, DateTime.Now, "Тестирование запущено");
			do { } while (!LoadFilesToDrive());

			while (_running)
			{
				if (DateTime.Now - _lastUpdateTime > _updatePeriod)
					do { } while (!LoadFilesToDrive());

				RunCheckCycle();
				SetErrorStatus();
			}
		}

		public void StopTest()
		{
			_parentWindow.SetBackgroundColor(Color.FromRgb(255, 255, 255));
			Utilities.LogEvent(_logFile, DateTime.Now, "Тестирование прервано");
			_running = false;
		}

		private void RunCheckCycle()
		{
			var filesFromDrive = new List<string>();
			
			try
			{
				var driveEnumeration = Utilities.Traverse(_drive.RootDirectory.FullName);

				foreach (var item in driveEnumeration)
				{
					var attribute = File.GetAttributes(item);
					if (attribute.HasFlag(FileAttributes.Directory))
						continue;

					var actualFilename = item.Substring(_drive.RootDirectory.FullName.Length,
						item.Length - _drive.RootDirectory.FullName.Length);
					if (!actualFilename.Contains("\\System Volume Information\\"))
						filesFromDrive.Add(actualFilename);
				}
			}
			catch (Exception)
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
							Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Не удалось сравнить версии файла " + file.Key);
							continue;
						}

						if (!identical)
						{
							_files[file.Key] = false;
							Utilities.LogEvent(_logFile, DateTime.Now, "Ошибка! Файл " + file.Key + " не совпадает с исходным");
							_errorsNum++;
							SetErrorStatus();
						}
					}
					else
					{
						Utilities.LogEvent(_logFile, DateTime.Now,  "Ошибка! Файл " + file.Key + "не найден");
						_errorsNum++;
						SetErrorStatus();
					}
				}
			}
			catch (Exception)
			{
			}
		}

		private bool LoadFilesToDrive()
		{
			_parentWindow.SetStatusText("идет форматирование...");
			if (!FormatDrive())
				return false;
			_parentWindow.SetStatusText("идет запись данных...");
			if (!WriteFilesToDrive())
				return false;
			_lastUpdateTime = DateTime.Now;
			Utilities.LogEvent(_logFile, _lastUpdateTime, "Выполнена плановая перезапись данных");
			SetErrorStatus();

			return true;
		}

		private void SetErrorStatus()
		{
			if (_errorsNum == 0)
			{
				_parentWindow.SetStatusText("выполняется, ошибок не найдено");
				_parentWindow.SetBackgroundColor(Color.FromRgb(191, 235, 171));
			}
			else
			{
				_parentWindow.SetStatusText("выполняется, обнаружено " + _errorsNum + " ошибок");
				_parentWindow.SetBackgroundColor(Color.FromRgb(245, 105, 105));
			}
		}

		private bool FormatDrive()
		{
			try
			{
				return Utilities.FormatDriveWithCmd(_drive.Name[0]);
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
