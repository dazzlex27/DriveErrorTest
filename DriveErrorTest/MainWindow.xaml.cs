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
				BtLaunchTesting.IsEnabled = true;
			}

			if (!CbDrives.Items.IsEmpty)
				return;

			 CbDrives.Items.Add("<Съемные диски не найдены>");
			SetGuiAccess(false);
			BtLaunchTesting.IsEnabled = false;
			BtShowLog.IsEnabled = false;
		}

		private void SetGuiAccess(bool active)
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

			if (dg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			_sourcePath = dg.SelectedPath;
			LbInputPath.Content = _sourcePath;
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
			SetGuiAccess(false);
			BtLaunchTesting.Content = "Остановить тестирование";
			SetTestingStatusText("запущено");
			SetGuiAccess(false);
		}

		private void StopTest()
		{
			UnsubscribeFromTesterEvents();
			_tester.StopTest();
			do { } while (_tester.IsRunning);
			BtLaunchTesting.Content = "Начать тестирование";
			SetTestingStatusText("остановлено");
			SetBackgroundColor(Color.FromRgb(255, 255, 255));
			SetTaskbarStatus(TaskbarItemProgressState.None, 0);
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
			Background = new SolidColorBrush(color);
		}

		public void SetTestingStatusText(string message)
		{
			LbStatusStrip.Content = message;
		}

		public void SetReadCyclesCountText(ulong cycles)
		{
			LbReadCyclesStrip.Content = cycles;
		}

		public void SetWriteCyclesCountText(ulong cycles)
		{
			LbWriteCyclesStrip.Content = cycles;
		}

		public void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			TaskbarItemInfo.ProgressState = state;
			TaskbarItemInfo.ProgressValue = value;
		}

		public void SetCurrentFileText(string filepath)
		{
			LbCurrFileStrip.Content = filepath;
		}

		public static int GetSelectedIndex(System.Windows.Controls.ComboBox combobox)
		{
			return combobox.SelectedIndex;
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
				CleanStart = CbCleanStart.IsChecked == true
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
					BtLaunchTesting.Content = "Запустить тестирование";
					LbStatusStrip.Content = "аварийно остановлено";
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
			var dg = new OpenFileDialog
			{
				Filter = "Файлы CSV (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt|Все доступные форматы|*.txt;*.csv"
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

			Environment.Exit(0);
		}
	}
}
