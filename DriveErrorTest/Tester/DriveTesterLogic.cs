using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DriveErrorTest.Tester
{
	internal class DriveTesterLogic
	{
		public event Action FormattingStarted;
		public event Action WritingStarted;
		public event Action ErrorOccured;
		public event Action WriteCycleCompleted;
		public event Action ReadCycleCompleted;

		private readonly DriveInfo _drive;
		private readonly DriveTesterSettings _settings;
		private readonly Dictionary<string, bool> _files;

		public DriveTesterLogic(DriveInfo drive, DriveTesterSettings settings)
		{
			_drive = drive;
			_settings = settings;
			_files = new Dictionary<string, bool>();
		}

		public void RunCheckCycle()
		{
			List<string> filesFromDrive;

			try
			{
				filesFromDrive = FileHelpers.GetFilesInDirectory(_drive.RootDirectory);
			}
			catch (Exception ex)
			{
				ErrorOccured?.Invoke();
				_settings.Log.LogException(DateTime.Now, "Не удалось построить список файлов на устройстве", ex.ToString());
				return;
			}

			if (filesFromDrive.Count != _files.Count)
			{
				ErrorOccured?.Invoke();
				_settings.Log.LogError(DateTime.Now,
					"Количество файлов не совпадает. Ожидалось - " + _files.Count + ", посчитано - " + filesFromDrive.Count);
			}

			try
			{
				CompareAllFiles(filesFromDrive);
			}
			catch (Exception ex)
			{
				ErrorOccured?.Invoke();
				_settings.Log.LogException(DateTime.Now, "Не удалось выполнить чтение с устройства", ex.ToString());
			}
			finally
			{
				Thread.Sleep(10);
			}
		}

		private void CompareAllFiles(IList<string> filesFromDrive)
		{
			foreach (var file in _files.ToArray().Where(file => file.Value))
			{
				var index = filesFromDrive.IndexOf(file.Key);
				if (index > -1)
				{
					bool identical;

					try
					{
						identical = FileHelpers.CompareTwoFiles(Path.Combine(_settings.SourceDirectory.FullName, file.Key),
							Path.Combine(_drive.RootDirectory.FullName, filesFromDrive[index]));
					}
					catch (Exception ex)
					{
						ErrorOccured?.Invoke();
						_files[file.Key] = false;
						_settings.Log.LogException(DateTime.Now, "Не удалось сравнить версии файла " + file.Key, ex.ToString());
						continue;
					}
					finally
					{
						ReadCycleCompleted?.Invoke();
					}

					if (identical)
						continue;

					ErrorOccured?.Invoke();
					_files[file.Key] = false;
					_settings.Log.LogError(DateTime.Now, "Файл " + file.Key + " не совпадает с исходным");
				}
				else
				{
					ErrorOccured?.Invoke();
					_files[file.Key] = false;
					_settings.Log.LogError(DateTime.Now, "Файл " + file.Key + " не найден");
				}
			}
		}

		public bool GetFilesFromSourceDirectory()
		{
			try
			{
				_files.Clear();
				var filesFromSourceDirectory = FileHelpers.GetFilesInDirectory(_settings.SourceDirectory);
				foreach (var actualFilename in filesFromSourceDirectory.Select(path => path.Substring(1, path.Length - 1)))
					_files.Add(actualFilename, true);

				return true;
			}
			catch (Exception ex)
			{
				_settings.Log.LogException(DateTime.Now, "Не удалось получить список файлов в источнике", ex.ToString());
				return false;
			}
		}

		public bool LoadFilesToDrive()
		{
			FormattingStarted?.Invoke();
			if (!FormatDrive())
			{
				ErrorOccured?.Invoke();
				return false;
			}

			WritingStarted?.Invoke();
			if (!GetFilesFromSourceDirectory())
			{
				ErrorOccured?.Invoke();
				return false;
			}

			if (_drive.RootDirectory.GetDirectories().Length > 1)
			{
				_settings.Log.LogError(DateTime.Now, "Устройство не отформатировано");
				return false;
			}

			if (!WriteFilesToDrive())
			{
				ErrorOccured?.Invoke();
				return false;
			}

			WriteCycleCompleted?.Invoke();
			return true;
		}

		private bool FormatDrive()
		{
			try
			{
				return FileHelpers.FormatDriveWithCmd(_drive.Name.Substring(0, 2), _drive.VolumeLabel);
			}
			catch (Exception ex)
			{
				_settings.Log.LogException(DateTime.Now, "Не удалось форматировать устройство", ex.ToString());
				return false;
			}
		}

		private bool WriteFilesToDrive()
		{
			try
			{
				// Create all of the directories
				foreach (var dirPath in Directory.GetDirectories(_settings.SourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
					Directory.CreateDirectory(dirPath.Replace(_settings.SourceDirectory.FullName, _drive.RootDirectory.FullName));

				// Copy all the files & Replaces any files with the same name
				foreach (var path in Directory.GetFiles(_settings.SourceDirectory.FullName, "*",
					SearchOption.AllDirectories))
				{
					File.Copy(path, path.Replace(_settings.SourceDirectory.FullName, _drive.RootDirectory.FullName), true);
				}

				return true;
			}
			catch (Exception ex)
			{
				_settings.Log.LogException(DateTime.Now, "Ошибка! Не удалось записать файлы на диск", ex.ToString());
				return false;
			}
		}
	}
}
