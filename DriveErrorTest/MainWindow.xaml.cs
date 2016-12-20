using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;

namespace DriveErrorTest
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private string _sourcePath = "";
		private string _logPath = "";
		private Thread _t;
		private Tester _tester;
		private bool _cleanStart;

		public MainWindow()
		{
			InitializeComponent();
			Initialize();
		}

		public ObservableCollection<DriveInfo> Drives { get; set; }

		private void Initialize()
		{
			Drives = new ObservableCollection<DriveInfo>();
			TaskbarItemInfo = new TaskbarItemInfo();
			GetDrivesList();

			CbTimePeriod.Items.Add("Раз в 10 минут");
			CbTimePeriod.Items.Add("Раз в час");
			CbTimePeriod.Items.Add("Раз в сутки");
			CbTimePeriod.Items.Add("Раз в двое суток");
			CbTimePeriod.Items.Add("Раз в трое суток");
			CbTimePeriod.Items.Add("Раз в неделю");

			CbDrives.SelectedIndex = 0;
			CbTimePeriod.SelectedIndex = 2;
		}

		private void GetDrivesList()
		{
			// TODO: bind combobox to collection
			Drives.Clear();
			CbDrives.Items.Clear();

			var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);

			foreach (var drive in drives.Where(drive => drive.DriveType == DriveType.Removable))
			{
				Drives.Add(drive);
				CbDrives.Items.Add(drive.Name + drive.VolumeLabel);
				SetUIStatus(true);
				BtLaunchTesting.IsEnabled = true;
			}

			if (CbDrives.Items.IsEmpty)
			{
				CbDrives.Items.Add("<Съемные диски не найдены>");
				SetUIStatus(false);
				BtLaunchTesting.IsEnabled = false;
				BtShowLog.IsEnabled = false;
			}
		}

		private void SetUIStatus(bool active)
		{
			CbTimePeriod.IsEnabled = active;
			CbDrives.IsEnabled = active;
			BtSelectSourcePath.IsEnabled = active;
			BtSelectLogPath.IsEnabled = active;
			CbCleanStart.IsEnabled = active;
		}

		private void BtSelectTestData_OnClick(object sender, RoutedEventArgs e)
		{
			var dg = new FolderBrowserDialog();

			if (dg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				_sourcePath = dg.SelectedPath;
				LbInputPath.Content = _sourcePath;
			}
		}

		private bool ReadyToTest()
		{
			return Directory.Exists(_sourcePath) && File.Exists(_logPath);
		}

		private void BtLaunchTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (_tester == null || !_tester.IsRunning)
			{
				if (ReadyToTest())
				{
					StartTest();
				}
				else
				{
					var message = "Не указаны следующие данные:";
					if (!Directory.Exists(_sourcePath))
						message += Environment.NewLine + "Путь к папке с исходными данными";
					if (!File.Exists(_logPath))
						message += Environment.NewLine + "Путь к журналу";
					System.Windows.MessageBox.Show(message, "Не хватает данных", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
			else
			{
				if (System.Windows.MessageBox.Show(
					"Вы действительно хотите прервать тестирование?",
					"Подтвердите действие",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					StopTest();
				}
			}
		}

		private void StartTest()
		{
			_t = new Thread(CreateTester);
			_t.Start();
			Title = Drives[CbDrives.SelectedIndex].Name + Drives[CbDrives.SelectedIndex].VolumeLabel;
			SetUIStatus(false);
			BtLaunchTesting.Content = "Остановить тестирование";
			SetTestingStatusText("запущено");
			SetUIStatus(false);
		}

		private void StopTest()
		{
			_tester.StopTest();
			do { } while (_tester.IsRunning); 
			BtLaunchTesting.Content = "Запустить тестирование";
			SetTestingStatusText("остановлено");
			SetBackgroundColor(Color.FromRgb(255, 255, 255));
			SetTaskbarStatus(TaskbarItemProgressState.None, 0);
			SetCurrentFile(" ");
			SetUIStatus(true);
		}

		public void BreakTestOnEmergency()
		{
			_tester.StopTest();
			TerminateTestingThread();
			Utilities.LogEvent(new FileInfo(_logPath),DateTime.Now,"Тестирование аварийно завершено. Число ошибок превысило 100");
			BtLaunchTesting.Content = "Запустить тестирование";
			LbStatusStrip.Content = "аварийно остановлено";
			SetBackgroundColor(Color.FromRgb(162, 0, 0));
			SetUIStatus(true);
		}

		private void TerminateTestingThread()
		{
			try { _t.Abort(); }
			catch { }
		}

		public void SetBackgroundColor(Color color)
		{
			try
			{
				if (Dispatcher.CheckAccess())
					Background = new SolidColorBrush(color);
				else
					Dispatcher.Invoke(new Action<Color>(SetBackgroundColor), color);
			}
			catch (Exception)
			{
				
			}
		}

		public void SetTestingStatusText(string message)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbStatusStrip.Dispatcher.CheckAccess())
						LbStatusStrip.Content = message;
					else
						LbStatusStrip.Dispatcher.Invoke(new Action<string>(SetTestingStatusText), message);
				}
				else
					Dispatcher.Invoke(new Action<string>(SetTestingStatusText), message);
			}
			catch (Exception)
			{
				
			}
			
		}

		public void SetWryteCycles(int cycles)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbWriteCyclesStrip.Dispatcher.CheckAccess())
						LbWriteCyclesStrip.Content = cycles;
					else
						LbWriteCyclesStrip.Dispatcher.Invoke(new Action<int>(SetWryteCycles), cycles);
				}
				else
					Dispatcher.Invoke(new Action<int>(SetWryteCycles), cycles);
			}
			catch (Exception)
			{

			}
		}

		public void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (TaskbarItemInfo.Dispatcher.CheckAccess())
					{
						TaskbarItemInfo.ProgressState = state;
						TaskbarItemInfo.ProgressValue = value;
					}
					else
						LbWriteCyclesStrip.Dispatcher.Invoke(new Action<TaskbarItemProgressState, double>(SetTaskbarStatus), state, value);
				}
				else
					Dispatcher.Invoke(new Action<TaskbarItemProgressState, double>(SetTaskbarStatus), state, value);
			}
			catch (Exception)
			{

			}
		}

		public void SetCurrentFile(string filepath)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbCurrFileStrip.Dispatcher.CheckAccess())
						LbCurrFileStrip.Content = filepath;
					else
						LbCurrFileStrip.Dispatcher.Invoke(new Action<string>(SetCurrentFile), filepath);
				}
				else
					Dispatcher.Invoke(new Action<string>(SetCurrentFile), filepath);
			}
			catch (Exception ex)
			{

			}
		}

		public static int GetSelectedIndex(System.Windows.Controls.ComboBox combobox)
		{
			if (combobox.Dispatcher.CheckAccess())
				return combobox.SelectedIndex;

			return (int) combobox.Dispatcher.Invoke(new Func<System.Windows.Controls.ComboBox, int>(GetSelectedIndex), combobox);
		}

		private void CreateTester()
		{
			TimeSpan span;

			switch (GetSelectedIndex(CbTimePeriod))
			{
				case 0:
					span = TimeSpan.FromMinutes(10);
					break;
				case 1:
					span = TimeSpan.FromHours(1);
					break;
				case 2:
					span = TimeSpan.FromDays(1);
					break;
				case 3:
					span = TimeSpan.FromDays(2);
					break;
				case 4:
					span = TimeSpan.FromDays(3);
					break;
				case 5:
					span = TimeSpan.FromDays(7);
					break;
				default:
					span = TimeSpan.FromDays(1);
					break;
			}

			_tester = new Tester(this, Drives[GetSelectedIndex(CbDrives)], _sourcePath, _logPath, span);
			_tester.CleanStart = _cleanStart;
			_tester.RunTest();
		}

		private void BtSelectLogPath_OnClick(object sender, RoutedEventArgs e)
		{
			var dg = new OpenFileDialog
			{
				Filter = "Файлы Excel (*.xls,*.xlsx)|*.xls;*.xlsx|Текстовые файлы (*.txt)|*.txt|Все доступные форматы|*.txt;*.xls;*.xlsx"
			};

			if (dg.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
			{
				_logPath = dg.FileName;
				LbLogPath.Content = _logPath;
				BtShowLog.IsEnabled = true;
			}
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (File.Exists(_logPath))
				Process.Start(_logPath);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_tester != null && _tester.IsRunning &&
			    System.Windows.MessageBox.Show(
				    "Вы действительно хотите прервать тестирование?",
				    "Подтвердите действие",
				    MessageBoxButton.YesNo,
				    MessageBoxImage.Question) == MessageBoxResult.No)
			{
				e.Cancel = true;
				return;
			}

			if (_tester != null && _tester.IsRunning)
			{
				StopTest();
				TerminateTestingThread();
			}

			Environment.Exit(0);
		}

		private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
		{
			_cleanStart = true;
		}

		private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
		{
			_cleanStart = false;
		}
	}
}
