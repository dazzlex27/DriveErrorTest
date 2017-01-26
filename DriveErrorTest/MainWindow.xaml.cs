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

			PopulateDriveGrid();
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
			if (AskBeforeExit())
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

			CbRewritePeriod.SelectedIndex = 2;
		}

		private void PopulateDriveGrid()
		{
			if (_driveManager.DriveList.Count == 0)
				return;

			foreach (var drive in _driveManager.DriveList)
			{
				// TODO: finish this
			}

			GrDrives.SelectedIndex = 0;
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
				(bool?)checkBox.Dispatcher.Invoke(new Func<System.Windows.Controls.CheckBox, bool?>(GetCheckBoxValue), checkBox);
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
			if (AskBeforeExit())
				ExitApp();
		}

		private void BtStart_Click(object sender, RoutedEventArgs e)
		{
			if (_driveManager.SourceDirectory != null)
			{
				if (GrDrives.SelectedIndex >= 0)
				{
					if (GrDrives.SelectedIndex >= 0)
						_driveManager.StartTest(GrDrives.SelectedIndex);
				}
			}
			else
			{
				MessageBox.Show(
					"Не указана папка-источник данных!",
					"Невозможно запустить тестирование",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
			}
		}

		private void BtPausehTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.PauseTest(GrDrives.SelectedIndex);
		}

		private void BtStop_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
			{
				if (AskToConfirmTestAbortion())
					_driveManager.StopTest(GrDrives.SelectedIndex);
			}
		}

		private bool AskBeforeExit()
		{
			if (_driveManager.TestsRunning)
			{
				if (AskToConfirmExit())
					return true;
			}
			else
			{
				if (AskToConfirmExit())
					return true;
			}

			return false;
		}
	}
}
