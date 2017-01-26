using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;

namespace DriveErrorTest
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private SystemTrayHelper _systemTrayHelper;
		private DriveManager _driveManager;

		public MainWindow()
		{
			InitializeComponent();
			InitializeAppLogic();
		}

		private void InitializeAppLogic()
		{
			if (!CheckIfMutexIsAvailable())
			{
				MessageBox.Show("Приложение уже запущено!", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				ExitApp();
			}

			InitializeTrayIcon();

			CommonLogger.Initialize();

			Title = GlobalContext.AppTitleTextBase;
			TaskbarItemInfo = new TaskbarItemInfo();
			PopulateComboboxes();

			InitializeDriveManager();

			if (_driveManager.DriveList.Count == 0)
				SetGuiAccess(false);
		}

		private static bool CheckIfMutexIsAvailable()
		{
			bool wasMutexCreated;
			new Mutex(true, "MutexForFDT", out wasMutexCreated);

			return wasMutexCreated;
		}

		private void InitializeTrayIcon()
		{
			_systemTrayHelper = new SystemTrayHelper();
			_systemTrayHelper.Initialize();
			_systemTrayHelper.ShowWindowEvent += SystemTrayHelper_ShowWindowEvent;
			_systemTrayHelper.ShutAppDownEvent += SystemTrayHelper_ShutAppDownEvent;
		}

		private void InitializeDriveManager()
		{
			_driveManager = new DriveManager();
			_driveManager.Initialize();
		}

		private void SystemTrayHelper_ShowWindowEvent()
		{
			if (IsVisible)
				Visibility = Visibility.Hidden;
			else
				Visibility = Visibility.Visible;
		}

		private void SystemTrayHelper_ShutAppDownEvent()
		{
			if (AskToConfirmExit())
				ExitApp();
		}

		private void SetGuiAccess(bool active)
		{
			if (Dispatcher.CheckAccess())
			{
				CbRewritePeriod.Dispatcher.Invoke(new Action(() => CbRewritePeriod.IsEnabled = active));
				BtSelectSourcePath.Dispatcher.Invoke(new Action(() => BtSelectSourcePath.IsEnabled = active));
				BtShowLog.Dispatcher.Invoke(new Action(() => BtShowLog.IsEnabled = active));
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

			_driveManager.SourceDirectory = new System.IO.DirectoryInfo(dg.SelectedPath);
			LbInputPath.Content = _driveManager.SourceDirectory.FullName;
		}

		private void BtStopAllDrives_OnClick(object sender, RoutedEventArgs e)
		{
			if (AskToConfirmTestAbortion())
				_driveManager.StopAllTests();
			//if (_driveTester == null || !_driveTester.IsRunning)
			//{
			//	if (ReadyToTest())
			//	{
			//		StartTest();
			//	}
			//	else
			//	{
			//		var message = "Не указаны следующие данные:";
			//		if (!Directory.Exists(_sourcePath))
			//			message += Environment.NewLine + "Путь к папке с исходными данными";
			//		if (!File.Exists(_logPath))
			//			message += Environment.NewLine + "Путь к журналу";
			//		MessageBox.Show(message, "Не хватает данных", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			//	}
			//}
			//else
			//{
			//	if (MessageBox.Show(
			//		"Вы действительно хотите прервать тестирование?",
			//		"Подтвердите действие",
			//		MessageBoxButton.YesNo,
			//		MessageBoxImage.Question) == MessageBoxResult.Yes)
			//	{
			//		StopTest();
			//	}
			//}
		}

		private void StartTest()
		{
			//try
			//{
			//	Title = GlobalContext.AppTitleTextBase + " - " + Drives[CbDrives.SelectedIndex].Name + Drives[CbDrives.SelectedIndex].VolumeLabel;
			//	_testingThread = new Thread(() => CreateTester(GetSelectedIndex(CbTimePeriod), GetCheckBoxValue(CbCleanStart) == true));
			//	_testingThread.Start();
			//	SetGuiAccess(false);
			//	SetStartStopButtonLabel(false);
			//	SetTestingStatusText("запущено");
			//	BtPausehTesting.Visibility = Visibility.Visible;
			//	SetGuiAccess(false);
			//}
			//catch (Exception)
			//{
			//	MessageBox.Show(
			//		"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства и файла журнала",
			//		"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			//}
		}

		private void StopTest()
		{
			//UnsubscribeFromTesterEvents();
			//_tester.StopTest();
			//do { } while (_tester.IsRunning);
			//SetStartStopButtonLabel(true);
			//SetTestingStatusText("остановлено");
			//SetBackgroundColor(Color.FromRgb(255, 255, 255));
			//SetTaskbarStatus(TaskbarItemProgressState.None, 0);
			//BtPausehTesting.Visibility = Visibility.Hidden;
			//SetCurrentFileText(" ");
			//SetGuiAccess(true);
		}

		public void SetBackgroundColor(Color color)
		{
			GUIHelpers.SetWindowBackgroundColor(this, color);
		}

		private void SetTestingStatusText(string message)
		{
			// TODO: fix
			GUIHelpers.SetLabelText(this, /* LbTestingStatusStrip */ null, message);
		}

		private void SetReadCyclesCountText(ulong cycles)
		{
			//TODO: fix
			GUIHelpers.SetLabelText(this, /* LbReadCyclesStrip */ null, cycles.ToString());
		}

		private void SetWriteCyclesCountText(ulong cycles)
		{
			//TODO: fix
			GUIHelpers.SetLabelText(this,/* LbWriteCyclesStrip */ null, cycles.ToString());
		}

		private void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			GUIHelpers.SetWindowTaskbarStatus(this, state, value);
		}

		private void SetCurrentFileText(string filepath)
		{
			//TODO: fix
			GUIHelpers.SetLabelText(this,/* LbCurrFileStrip */ null, filepath);
		}

		private void PopulateComboboxes()
		{
			CbRewritePeriod.Items.Add("Раз в 10 минут");
			CbRewritePeriod.Items.Add("Раз в час");
			CbRewritePeriod.Items.Add("Раз в 3 часа");
			CbRewritePeriod.Items.Add("Раз в 6 часов");
			CbRewritePeriod.Items.Add("Раз в 12 часов");
			CbRewritePeriod.Items.Add("Раз в сутки");
			CbRewritePeriod.Items.Add("Раз в двое суток");
			CbRewritePeriod.Items.Add("Раз в трое суток");
			CbRewritePeriod.Items.Add("Раз в неделю");

			//CbDrives.SelectedIndex = 0;
			CbRewritePeriod.SelectedIndex = 2;
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
			//TimeSpan span;

			//switch (periodValue)
			//{
			//	case 0:
			//		span = TimeSpan.FromMinutes(10);
			//		break;
			//	case 1:
			//		span = TimeSpan.FromHours(1);
			//		break;
			//	case 2:
			//		span = TimeSpan.FromHours(3);
			//		break;
			//	case 3:
			//		span = TimeSpan.FromHours(6);
			//		break;
			//	case 4:
			//		span = TimeSpan.FromHours(12);
			//		break;
			//	case 5:
			//		span = TimeSpan.FromDays(1);
			//		break;
			//	case 6:
			//		span = TimeSpan.FromDays(2);
			//		break;
			//	case 7:
			//		span = TimeSpan.FromDays(3);
			//		break;
			//	case 8:
			//		span = TimeSpan.FromDays(7);
			//		break;
			//	default:
			//		span = TimeSpan.FromDays(1);
			//		break;
			//}

			//_tester = new Tester(new Logger(_logPath), Drives[GetSelectedIndex(CbDrives)], _sourcePath, span)
			//{
			//	CleanStart = cleanStart
			//};

			//SubscribeToTesterEvents();

			//_tester.RunTest();
		}

		private void SubscribeToTesterEvents()
		{
			//if (_driveTester == null)
			//	return;

			//_driveTester.OnErrorCountChanged += OnErrorCountChangedEventHandler;
			//_driveTester.OnCurrentFileChanged += OnCurrentFileChangedEventHandler;
			//_driveTester.OnReadCyclesCountChanged += OnReadCyclesCountChangedEventHandler;
			//_driveTester.OnWriteCyclesCountChanged += OnWriteCyclesCountChangedEventHandler;
			//_driveTester.OnTestingStatusChanged += OnTestingStatusChangedEventHandler;
		}

		private void UnsubscribeFromTesterEvents()
		{
			//if (_driveTester == null)
			//	return;

			//_driveTester.OnErrorCountChanged -= OnErrorCountChangedEventHandler;
			//_driveTester.OnCurrentFileChanged -= OnCurrentFileChangedEventHandler;
			//_driveTester.OnReadCyclesCountChanged -= OnReadCyclesCountChangedEventHandler;
			//_driveTester.OnWriteCyclesCountChanged -= OnWriteCyclesCountChangedEventHandler;
			//_driveTester.OnTestingStatusChanged -= OnTestingStatusChangedEventHandler;
		}

		private void DisposeComponents()
		{
			_driveManager.StopAllTests();
			_systemTrayHelper?.Dispose();
		}

		private void ExitApp()
		{
			DisposeComponents();
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
			//if (errorsCount == 0)
			//{
			//	SetTestingStatusText("ошибок не найдено...");
			//	SetBackgroundColor(Color.FromRgb(191, 235, 171));
			//	SetTaskbarStatus(TaskbarItemProgressState.Normal, 1);
			//}
			//else
			//{
			//	if (errorsCount >= 100)
			//	{
			//		SetStartStopButtonLabel(true);
			//		SetBackgroundColor(Color.FromRgb(162, 0, 0));
			//		SetGuiAccess(true);
			//		return;
			//	}

			//	SetTestingStatusText($"обнаружено {_driveTester.ErrorsCount} ошибок");
			//	SetBackgroundColor(Color.FromRgb(245, 105, 105));
			//	SetTaskbarStatus(TaskbarItemProgressState.Error, 1);
			//}
		}

		private void BtSelectLogPath_OnClick(object sender, RoutedEventArgs e)
		{
			//var dg = new System.Windows.Forms.OpenFileDialog
			//{
			//	Filter = "Текстовые файлы (*.txt)|*.txt|Файлы CSV (*.csv)|*.csv|Все доступные форматы|*.txt;*.csv"
			//};

			//if (dg.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
			//	return;

			//_logPath = dg.FileName;
			//LbLogPath.Content = _logPath;
			//BtShowLog.IsEnabled = true;
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.ShowLogSelected(GrDrives.SelectedIndex);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Visibility = Visibility.Hidden;
			e.Cancel = true;
		}

		private static bool AskToConfirmTestAbortion()
		{
			return MessageBox.Show(
					"Вы действительно хотите прервать тестирование?",
					"Подтвердите действие",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question) == MessageBoxResult.Yes;
		}

		private static bool AskToConfirmExit()
		{
			return MessageBox.Show(
				"Вы действительно хотите выйти?",
				"Завершение тестирования",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question) == MessageBoxResult.Yes;
		}

		private void MenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			if (AskToConfirmExit())
				ExitApp();
		}

		private void BtStart_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtPausehTesting_OnClick(object sender, RoutedEventArgs e)
		{

		}

		private void BtStop_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
