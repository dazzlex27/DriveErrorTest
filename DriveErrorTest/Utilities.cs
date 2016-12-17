using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using Excel = Microsoft.Office.Interop.Excel;

namespace DriveErrorTest
{
	public static class Utilities
	{
		public static bool FormatDriveWithCmd(char driveLetter, string label = "", string fileSystem = "NTFS",
			bool quickFormat = true, bool enableCompression = false, int? clusterSize = null)
		{
			var success = false;
			var drive = driveLetter + ":";
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "format.com",
					CreateNoWindow = true,
					WorkingDirectory = Environment.SystemDirectory,
					Arguments = "/FS:" + fileSystem +
					            " /Y" +
					            " /V:" + label +
					            (quickFormat ? " /Q" : "") +
					            ((fileSystem == "NTFS" && enableCompression) ? " /C" : "") +
					            (clusterSize.HasValue ? " /A:" + clusterSize.Value : "") +
					            " " + drive,
					UseShellExecute = false
				};
				psi.CreateNoWindow = true;
				psi.RedirectStandardOutput = true;
				psi.RedirectStandardInput = true;
				var formatProcess = Process.Start(psi);
				var swStandardInput = formatProcess.StandardInput;
				swStandardInput.WriteLine();
				formatProcess.WaitForExit();
				success = true;
			}
			catch (Exception)
			{
				
			}
			return success;
		}

		public static IEnumerable<string> Traverse(string rootDirectory)
		{
			var files = Enumerable.Empty<string>();
			var directories = Enumerable.Empty<string>();
			try
			{
				// The test for UnauthorizedAccessException.
				var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
				permission.Demand();

				files = Directory.GetFiles(rootDirectory);
				directories = Directory.GetDirectories(rootDirectory);
			}
			catch
			{
				// Ignore folder (access denied).
				rootDirectory = null;
			}

			if (rootDirectory != null)
				yield return rootDirectory;

			foreach (var file in files)
			{
				yield return file;
			}

			// Recursive call for SelectMany.
			var subdirectoryItems = directories.SelectMany(Traverse);
			foreach (var result in subdirectoryItems)
			{
				yield return result;
			}
		}

		public static bool CompareTwoFiles(string filepath1, string filepath2)
		{
			var bytes1 = File.ReadAllBytes(filepath1);
			var bytes2 = File.ReadAllBytes(filepath2);

			if (bytes1.Length != bytes2.Length)
				return false;

			return !bytes1.Where((t, i) => t != bytes2[i]).Any();
		}

		public static void WriteToTxtFile(string filepath, string message)
		{
			using (var sw = new StreamWriter(filepath, true))
			{
				sw.WriteLine(message);
				sw.Close();
			}
		}

		public static void WriteToExcelFile(string filepath, DateTime timestamp, string message)
		{
			try
			{
				var excelApp = new Excel.Application { Visible = false, DisplayAlerts = false };
				var workbook = excelApp.Workbooks.Open(filepath);
				var worksheet = (Excel.Worksheet)workbook.Worksheets.Item[1];
				Excel.Range last = worksheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
				Excel.Range range = worksheet.get_Range("A1", last);
				int lastUsedRow = last.Row;
				worksheet.Cells[lastUsedRow + 1, 1] = timestamp.ToShortDateString();
				worksheet.Cells[lastUsedRow + 1, 2] = timestamp.ToLongTimeString();
				worksheet.Cells[lastUsedRow + 1, 3] = message;
				workbook.SaveAs(filepath, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing, Type.Missing);
				workbook.Close();
				excelApp.Quit();
			}
			catch (Exception)
			{
				
			}
		}

		public static void LogEvent(FileInfo logpath, DateTime timestamp, string message)
		{
			if (!logpath.Exists)
				File.Create(logpath.FullName);

			var timestampString = timestamp.ToShortDateString() + " " + timestamp.ToLongTimeString();

			switch (logpath.Extension)
			{
				case ".txt":
					WriteToTxtFile(logpath.FullName,
						timestampString + " " + message);
					break;
				case ".xls":
					WriteToExcelFile(logpath.FullName, timestamp, message);
					break;
				case ".xlsx":
					WriteToExcelFile(logpath.FullName, timestamp, message);
					break;
				default:
					throw new Exception("Неподдерживаемый формат лог файла!");
			}
		}

	}
}
