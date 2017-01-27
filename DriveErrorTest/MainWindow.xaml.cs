using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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

		public List<TimeSpan> Spans;  

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

		private void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			GUIHelpers.SetWindowTaskbarStatus(this, state, value);
		}

		private void PopulateComboboxes()
		{
			Spans = new List<TimeSpan>
			{
				new TimeSpan(0, 0, 10, 0),
				new TimeSpan(0, 1, 0, 0),
				new TimeSpan(0, 3, 0, 0),
				new TimeSpan(0, 6, 0, 0),
				new TimeSpan(0, 12, 0, 0),
				new TimeSpan(1, 0, 0, 0),
				new TimeSpan(2, 0, 0, 0),
				new TimeSpan(3, 0, 0, 0),
				new TimeSpan(7, 0, 0, 0)
			};

			CbRewritePeriod.ItemsSource = Spans;
			CbRewritePeriod.SelectedIndex = 2;
		}

		private void PopulateDriveGrid()
		{
			GrDrives.ItemsSource = _driveManager.DriveList;

			if (_driveManager.DriveList.Count == 0)
				return;

			GrDrives.SelectedIndex = 0;
		}

		public static int GetSelectedIndex(ComboBox combobox)
		{
			if (combobox.Dispatcher.CheckAccess())
				return combobox.SelectedIndex;

			return (int)combobox.Dispatcher.Invoke(new Func<ComboBox, int>(GetSelectedIndex), combobox);
		}

		public static bool? GetCheckBoxValue(CheckBox checkBox)
		{
			if (checkBox.Dispatcher.CheckAccess())
				return checkBox.IsChecked;

			return
				(bool?)checkBox.Dispatcher.Invoke(new Func<CheckBox, bool?>(GetCheckBoxValue), checkBox);
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

		private bool AskBeforeExit()
		{
			if (_driveManager.TestsRunning)
			{
				if (AskToConfirmTestAbortion())
					return true;
			}
			else
			{
				if (AskToConfirmExit())
					return true;
			}

			return false;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Visibility = Visibility.Hidden;
			e.Cancel = true;
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
				if (GrDrives.SelectedIndex < 0)
					return;

				try
				{
					_driveManager.StartTest(GrDrives.SelectedIndex);
					SetGuiAccess(false);
				}
				catch
				{
					MessageBox.Show(
						"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

		private void BtPauseTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.PauseTest(GrDrives.SelectedIndex);
		}

		private void BtStop_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
			{
				if (_driveManager.TestsRunning)
				{
					if (AskToConfirmTestAbortion())
						_driveManager.StopTest(GrDrives.SelectedIndex);
				}
			}
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.ShowLogSelected(GrDrives.SelectedIndex);
		}

		private void GrDrives_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyName == "Settings" || e.PropertyName == "Running")
				e.Cancel = true;
		}
	}
}
