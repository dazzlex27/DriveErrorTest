using System;
using System.IO;
using System.Text;

namespace DriveErrorTest
{
	internal class Logger
	{
		private enum EventType
		{
			Error,
			Info,
			Exception
		}

		private FileInfo _fileInfo;

		public Logger(string path)
		{
			_fileInfo = new FileInfo(path);
		}

		public void LogInfo(DateTime timestamp, string message)
		{
			LogEvent(timestamp, EventType.Info, message);
		}

		public void LogError(DateTime timestamp, string message)
		{
			LogEvent(timestamp, EventType.Error, message);
		}

		public void LogException(DateTime timestamp, string message, string exceptionText)
		{
			LogEvent(timestamp, EventType.Exception, message, exceptionText);
		}

		private void LogEvent(DateTime timestamp, EventType eventType, string message, string exceptionText = "")
		{
			if (!_fileInfo.Exists)
				File.Create(_fileInfo.FullName);

			string eventString;

			switch (eventType)
			{
				case EventType.Info:
					eventString = "Информация";
					break;
					case EventType.Error:
					eventString = "Ошибка";
					break;
					case EventType.Exception:
					eventString = "Исключительная ситуация";
					break;
				default:
					eventString = "";
					break;
			}

			switch (_fileInfo.Extension)
			{
				case ".txt":
					var timestampString = timestamp.ToShortDateString() + " " + timestamp.ToLongTimeString();
					WriteToTxtFile(timestampString + " " + eventString + " " + message +
					               (exceptionText == "" ? "" : (" Текст ошибки: " + exceptionText)));
					break;
				case ".csv":
					WriteToCsv(timestamp, eventString, message, exceptionText);
					break;
				default:
					throw new Exception("Неподдерживаемый формат лог файла!");
			}
		}

		private void WriteToCsv(DateTime timestamp, string eventString, string message, string exceptionText)
		{
			try
			{
				const char delimiter = ';';
				var result = Environment.NewLine + timestamp.ToShortDateString() + delimiter + timestamp.ToLongTimeString() +
				             delimiter + eventString + delimiter + message +
				             (exceptionText == ""
					             ? ""
					             : delimiter + exceptionText);
				File.AppendAllText(_fileInfo.FullName, result, Encoding.Default);
			}
			catch (Exception)
			{
			}
		}

		private void WriteToTxtFile(string message)
		{
			using (var sw = new StreamWriter(_fileInfo.FullName, true))
			{
				sw.WriteLine(message);
				sw.Close();
			}
		}
	}
}
