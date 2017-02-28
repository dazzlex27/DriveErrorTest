using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

namespace DriveErrorTest
{
	public partial class MainWindow
	{
		private MainWindowVm _viewModel;

		public MainWindow()
		{
			if (!CheckIfMutexIsAvailable())
			{
				MessageBox.Show("Приложение уже запущено!", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Application.Current.Shutdown();
			}

			_viewModel = (MainWindowVm)DataContext;
			_viewModel.ErrorOccured += ViewModel_ErrorOccured;
			_viewModel.ShowWindowRequested += ViewModel_ShowWindowRequested;

			InitializeComponent();
			InitializeAppComponents();

			Title = GlobalContext.AppTitleTextBase;
			TaskbarItemInfo = new TaskbarItemInfo();
		}

		private void ViewModel_ShowWindowRequested()
		{
			Visibility = IsVisible ? Visibility.Hidden : Visibility.Visible;
		}

		private void ViewModel_ErrorOccured(string exception, string message)
		{
			CommonLogger.LogException("Failed to start test: " + exception);
			MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void InitializeAppComponents()
		{
			if (_viewModel.DriveManager.DriveList.Count == 0)
				SetGuiAccess(false);
			else
				GrDrives.SelectedIndex = 0;
		}

		private static bool CheckIfMutexIsAvailable()
		{
			bool wasMutexCreated;
			new Mutex(true, "MutexForFDT", out wasMutexCreated);

			return wasMutexCreated;
		}

		private void SetGuiAccess(bool active)
		{
			if (Dispatcher.CheckAccess())
			{
				CbRewritePeriod.Dispatcher.Invoke(new Action(() => CbRewritePeriod.IsEnabled = active));
				BtSelectSourcePath.Dispatcher.Invoke(new Action(() => BtSelectSourcePath.IsEnabled = active));
				CbCleanStart.Dispatcher.Invoke(new Action(() => CbCleanStart.IsEnabled = active));
			}
			else
				Dispatcher.Invoke(new Action<bool>(SetGuiAccess), active);
		}

		public void SetBackgroundColor(Color color)
		{
			GUIHelpers.SetWindowBackgroundColor(this, color);
		}

		private void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			GUIHelpers.SetWindowTaskbarStatus(this, state, value);
		}

		private bool ConfirmExit()
		{
			if (_viewModel.TestsRunning)
			{
				if (GUIHelpers.AskToConfirmTestAbortion())
					return true;
			}
			else
			{
				if (GUIHelpers.AskToConfirmExit())
					return true;
			}

			return false;
		}

		private void MenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			if (ConfirmExit())
				_viewModel.ExitApp();
		}

		private void BtStart_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex < 0)
				return;

			_viewModel.TryToStartTest(GrDrives.SelectedItem);
		}

		private void BtPauseTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_viewModel.DriveManager.PauseTest(GrDrives.SelectedItem);
		}

		private void BtStop_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex < 0)
				return;

			if (!_viewModel.DriveManager.TestsRunning)
				return;

			if (GUIHelpers.AskToConfirmTestAbortion())
				_viewModel.DriveManager.StopTest(GrDrives.SelectedItem);
		}

		private void GrDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (GrDrives.SelectedIndex < 0)
				return;

			var item = (DriveInfoStorage)GrDrives.SelectedItem;
			if (item.Running)
			{
				CbCleanStart.IsEnabled = false;
				CbRewritePeriod.IsEnabled = false;
			}
			else
			{
				CbCleanStart.IsEnabled = true;
				CbRewritePeriod.IsEnabled = true;
			}

			_viewModel.SelectedDrive = item;
		}

		private void BtStartAllDrives_OnClick(object sender, RoutedEventArgs e)
		{
			_viewModel.StartAllTests();
		}

		private void BtStopAllDrives_OnClick(object sender, RoutedEventArgs e)
		{
			if (ConfirmExit())
				_viewModel.DriveManager.StopAllTests();
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_viewModel.DriveManager.ShowLogSelected(GrDrives.SelectedItem);
		}

		private void BtSelectTestData_OnClick(object sender, RoutedEventArgs e)
		{
			var dg = new System.Windows.Forms.FolderBrowserDialog();

			if (dg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			_viewModel.DriveManager.SourceDirectory = new System.IO.DirectoryInfo(dg.SelectedPath);
			LbInputPath.Content = _viewModel.DriveManager.SourceDirectory.FullName;
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			Visibility = Visibility.Hidden;
			e.Cancel = true;
		}
	}
}
