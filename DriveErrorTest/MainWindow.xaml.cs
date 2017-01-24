using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using System.Reflection;

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
		private SystemTrayHelper _systemTrayHelper;

		public MainWindow()
		{
			InitializeComponent();
			Initialize();
		}

		public ObservableCollection<DriveInfo> Drives { get; set; }

		private void Initialize()
		{
			bool wasMutexCreated;
			new Mutex(true, "MutexForFDT", out wasMutexCreated);

			if (!wasMutexCreated)
			{
				MessageBox.Show("Приложение уже запущено!", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				ExitApp();
			}

			_systemTrayHelper = new SystemTrayHelper();
			Title = GlobalContext.AppTitleTextBase;
			Drives = new ObservableCollection<DriveInfo>();
			TaskbarItemInfo = new TaskbarItemInfo();
			GetDrivesList();

			CbTimePeriod.Items.Add("Раз в 10 минут");
			CbTimePeriod.Items.Add("Раз в час");
			CbTimePeriod.Items.Add("Раз в 3 часа");
			CbTimePeriod.Items.Add("Раз в 6 часов");
			CbTimePeriod.Items.Add("Раз в 12 часов");
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
				SetGuiAccess(true);
				BtStartStopTesting.IsEnabled = true;
			}

			if (!CbDrives.Items.IsEmpty)
				return;

			 CbDrives.Items.Add("<Съемные диски не найдены>");
			SetGuiAccess(false);
			BtStartStopTesting.IsEnabled = false;
			BtShowLog.IsEnabled = false;
		}

		private void SetGuiAccess(bool active)
		{
			if (Dispatcher.CheckAccess())
			{
				CbTimePeriod.Dispatcher.Invoke(new Action(() => CbTimePeriod.IsEnabled = active));
				CbDrives.Dispatcher.Invoke(new Action(() => CbDrives.IsEnabled = active));
				BtSelectSourcePath.Dispatcher.Invoke(new Action(() => BtSelectSourcePath.IsEnabled = active));
				BtSelectLogPath.Dispatcher.Invoke(new Action(() => BtSelectLogPath.IsEnabled = active));
				CbCleanStart.Dispatcher.Invoke(new Action(() => CbCleanStart.IsEnabled = active));
			}
			else
				Dispatcher.Invoke(new Action<bool>(SetGuiAccess), active);
		}

		private void BtSelectTestData_OnClick(object sender, RoutedEventArgs e)
		{
			var dg = new System.Windows.Forms.FolderBrowserDialog();

			if (dg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			_sourcePath = dg.SelectedPath;
			LbInputPath.Content = _sourcePath;
		}

		private bool ReadyToTest()
		{
			return Directory.Exists(_sourcePath) && File.Exists(_logPath);
		}

		private void BtStartStopTesting_OnClick(object sender, RoutedEventArgs e)
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
			try
			{
				Title = GlobalContext.AppTitleTextBase + " - " + Drives[CbDrives.SelectedIndex].Name + Drives[CbDrives.SelectedIndex].VolumeLabel;
				_t = new Thread(() => CreateTester(GetSelectedIndex(CbTimePeriod), GetCheckBoxValue(CbCleanStart) == true));
				_t.Start();
				SetGuiAccess(false);
				SetStartStopButtonLabel(false);
				SetTestingStatusText("запущено");
				BtPausehTesting.Visibility = Visibility.Visible;
				SetGuiAccess(false);
			}
			catch (Exception)
			{
				MessageBox.Show(
					"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства и файла журнала",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void StopTest()
		{
			UnsubscribeFromTesterEvents();
			_tester.StopTest();
			do { } while (_tester.IsRunning);
			SetStartStopButtonLabel(true);
			SetTestingStatusText("остановлено");
			SetBackgroundColor(Color.FromRgb(255, 255, 255));
			SetTaskbarStatus(TaskbarItemProgressState.None, 0);
			BtPausehTesting.Visibility = Visibility.Hidden;
			SetCurrentFileText(" ");
			SetGuiAccess(true);
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

		private void SetTestingStatusText(string message)
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

		private void SetReadCyclesCountText(ulong cycles)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbReadCyclesStrip.Dispatcher.CheckAccess())
						LbReadCyclesStrip.Content = cycles;
					else
						LbReadCyclesStrip.Dispatcher.Invoke(new Action<ulong>(SetReadCyclesCountText), cycles);
				}
				else
					Dispatcher.Invoke(new Action<ulong>(SetReadCyclesCountText), cycles);
			}
			catch (Exception)
			{
			}
		}

		private void SetWriteCyclesCountText(ulong cycles)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbWriteCyclesStrip.Dispatcher.CheckAccess())
						LbWriteCyclesStrip.Content = cycles;
					else
						LbWriteCyclesStrip.Dispatcher.Invoke(new Action<ulong>(SetWriteCyclesCountText), cycles);
				}
				else
					Dispatcher.Invoke(new Action<ulong>(SetWriteCyclesCountText), cycles);
			}
			catch (Exception)
			{
			}
		}

		private void SetTaskbarStatus(TaskbarItemProgressState state, double value)
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

		private void SetCurrentFileText(string filepath)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (LbCurrFileStrip.Dispatcher.CheckAccess())
						LbCurrFileStrip.Content = filepath;
					else
						LbCurrFileStrip.Dispatcher.Invoke(new Action<string>(SetCurrentFileText), filepath);
				}
				else
					Dispatcher.Invoke(new Action<string>(SetCurrentFileText), filepath);
			}
			catch (Exception)
			{
			}
		}

		private void SetStartStopButtonLabel(bool start)
		{
			try
			{
				if (Dispatcher.CheckAccess())
				{
					if (BtStartStopTesting.Dispatcher.CheckAccess())
					{
						BtStartStopTesting.Content = start ? "Начать" : "Остановить";
					}
					else
						LbCurrFileStrip.Dispatcher.Invoke(new Action<bool>(SetStartStopButtonLabel), start);
				}
				else
					Dispatcher.Invoke(new Action<bool>(SetStartStopButtonLabel), start);
			}
			catch (Exception)
			{
			}
		}

		public static int GetSelectedIndex(System.Windows.Controls.ComboBox combobox)
		{
			if (combobox.Dispatcher.CheckAccess())
				return combobox.SelectedIndex;

			return (int)combobox.Dispatcher.Invoke(new Func<System.Windows.Controls.ComboBox, int>(GetSelectedIndex), combobox);
		}

		public static bool? GetCheckBoxValue(System.Windows.Controls.CheckBox checkBox)
		{
			if (checkBox.Dispatcher.CheckAccess())
				return checkBox.IsChecked;

			return
				(bool?) checkBox.Dispatcher.Invoke(new Func<System.Windows.Controls.CheckBox, bool?>(GetCheckBoxValue), checkBox);
		}

		private void CreateTester(int periodValue, bool cleanStart)
		{
			TimeSpan span;

			switch (periodValue)
			{
				case 0:
					span = TimeSpan.FromMinutes(10);
					break;
				case 1:
					span = TimeSpan.FromHours(1);
					break;
				case 2:
					span = TimeSpan.FromHours(3);
					break;
				case 3:
					span = TimeSpan.FromHours(6);
					break;
				case 4:
					span = TimeSpan.FromHours(12);
					break;
				case 5:
					span = TimeSpan.FromDays(1);
					break;
				case 6:
					span = TimeSpan.FromDays(2);
					break;
				case 7:
					span = TimeSpan.FromDays(3);
					break;
				case 8:
					span = TimeSpan.FromDays(7);
					break;
				default:
					span = TimeSpan.FromDays(1);
					break;
			}

			_tester = new Tester(new Logger(_logPath), Drives[GetSelectedIndex(CbDrives)], _sourcePath, span)
			{
				CleanStart = cleanStart
			};

			SubscribeToTesterEvents();

			_tester.RunTest();
		}

		private void SubscribeToTesterEvents()
		{
			if (_tester == null)
				return;

			_tester.OnErrorCountChanged += OnErrorCountChangedEventHandler;
			_tester.OnCurrentFileChanged += OnCurrentFileChangedEventHandler;
			_tester.OnReadCyclesCountChanged += OnReadCyclesCountChangedEventHandler;
			_tester.OnWriteCyclesCountChanged += OnWriteCyclesCountChangedEventHandler;
			_tester.OnTestingStatusChanged += OnTestingStatusChangedEventHandler;
		}

		private void UnsubscribeFromTesterEvents()
		{
			if (_tester == null)
				return;

			_tester.OnErrorCountChanged -= OnErrorCountChangedEventHandler;
			_tester.OnCurrentFileChanged -= OnCurrentFileChangedEventHandler;
			_tester.OnReadCyclesCountChanged -= OnReadCyclesCountChangedEventHandler;
			_tester.OnWriteCyclesCountChanged -= OnWriteCyclesCountChangedEventHandler;
			_tester.OnTestingStatusChanged -= OnTestingStatusChangedEventHandler;
		}

		private void ExitApp()
		{
			if (_tester.IsRunning)
				StopTest();
			_systemTrayHelper.Dispose();
			// TODO: exit more gracefully than that
			Environment.Exit(0);
		}

		private void OnTestingStatusChangedEventHandler(string statusText)
		{
			SetTestingStatusText(statusText);
		}

		private void OnWriteCyclesCountChangedEventHandler(ulong cyclesCount)
		{
			SetWriteCyclesCountText(cyclesCount);
		}

		private void OnReadCyclesCountChangedEventHandler(ulong cyclesCount)
		{
			SetReadCyclesCountText(cyclesCount);
		}

		private void OnCurrentFileChangedEventHandler(string filename)
		{
			SetCurrentFileText(filename);
		}

		private void OnErrorCountChangedEventHandler(int errorsCount)
		{
			if (errorsCount == 0)
			{
				SetTestingStatusText("ошибок не найдено...");
				SetBackgroundColor(Color.FromRgb(191, 235, 171));
				SetTaskbarStatus(TaskbarItemProgressState.Normal, 1);
			}
			else
			{
				if (errorsCount >= 100)
				{
					SetStartStopButtonLabel(true);
					SetBackgroundColor(Color.FromRgb(162, 0, 0));
					SetGuiAccess(true);
					return;
				}

				SetTestingStatusText($"обнаружено {_tester.ErrorsCount} ошибок");
				SetBackgroundColor(Color.FromRgb(245, 105, 105));
				SetTaskbarStatus(TaskbarItemProgressState.Error, 1);
			}
		}

		private void BtSelectLogPath_OnClick(object sender, RoutedEventArgs e)
		{
			var dg = new System.Windows.Forms.OpenFileDialog
			{
				Filter = "Текстовые файлы (*.txt)|*.txt|Файлы CSV (*.csv)|*.csv|Все доступные форматы|*.txt;*.csv"
			};

			if (dg.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			_logPath = dg.FileName;
			LbLogPath.Content = _logPath;
			BtShowLog.IsEnabled = true;
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

			ExitApp();
		}

		private void MenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ExitApp();
		}

		private void BtPausehTesting_OnClick(object sender, RoutedEventArgs e)
		{
			
		}
	}
}
