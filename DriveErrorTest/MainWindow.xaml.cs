﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	public partial class MainWindow : INotifyPropertyChanged
	{
		private SystemTrayHelper _systemTrayHelper;
		private DriveManager _driveManager;
		private DriveInfoStorage _selectedDrive;

		public List<TimeSpan> Spans { get; set; }

		public DriveInfoStorage SelectedDrive
		{
			get { return _selectedDrive; }
			set
			{
				_selectedDrive = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedDrive"));
			}
		}

		public MainWindow()
		{
			CreateSpansList();
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

		public void SetBackgroundColor(Color color)
		{
			GUIHelpers.SetWindowBackgroundColor(this, color);
		}

		private void SetTaskbarStatus(TaskbarItemProgressState state, double value)
		{
			GUIHelpers.SetWindowTaskbarStatus(this, state, value);
		}

		private void CreateSpansList()
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
		}

		private void PopulateDriveGrid()
		{
			GrDrives.ItemsSource = _driveManager.DriveList;

			if (_driveManager.DriveList.Count == 0)
				return;

			GrDrives.SelectedIndex = 0;
		}

		private void DisposeComponents()
		{
			foreach (var item in _driveManager.DriveList)
				_driveManager.StopTest(item);
			_systemTrayHelper?.Dispose();
		}

		private void TryToStartTest(object item)
		{
			var temp = item as DriveInfoStorage;

			if (_driveManager.SourceDirectory != null)
			{
				try
				{
					_driveManager.StartTest(temp);
				}
				catch (Exception ex)
				{
					CommonLogger.LogException("Failed to start test: " + ex);
					MessageBox.Show(
						"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства" + temp?.Name,
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else
			{
				GUIHelpers.ShowNoSourceSelectedMessage();
			}
		}

		private bool AskBeforeExit()
		{
			if (_driveManager.TestsRunning)
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

		private void ExitApp()
		{
			DisposeComponents();
			// TODO: exit more gracefully than that
			Environment.Exit(0);
		}

		private void MenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			if (AskBeforeExit())
				ExitApp();
		}

		private void BtStart_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex < 0)
				return;

			TryToStartTest(GrDrives.SelectedItem);
		}

		private void BtPauseTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.PauseTest(GrDrives.SelectedItem);
		}

		private void BtStop_Click(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex < 0)
				return;

			if (!_driveManager.TestsRunning)
				return;

			if (GUIHelpers.AskToConfirmTestAbortion())
				_driveManager.StopTest(GrDrives.SelectedItem);
		}

		private void GrDrives_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyName == "Settings" || e.PropertyName == "Running")
				e.Cancel = true;

			var displayName = GUIHelpers.GetPropertyDisplayName(e.PropertyDescriptor);

			if (!string.IsNullOrEmpty(displayName))
				e.Column.Header = displayName;

			DataGridComboBoxColumn col = e.Column as DataGridComboBoxColumn;
			if (col != null)
			{
				//col.DisplayMemberPath = "Description";
			}
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

			SelectedDrive = item;
		}
		

		private void BtStartAllDrives_OnClick(object sender, RoutedEventArgs e)
		{
			foreach (var item in _driveManager.DriveList)
				TryToStartTest(item);
		}

		private void BtStopAllDrives_OnClick(object sender, RoutedEventArgs e)
		{
			if (GUIHelpers.AskToConfirmTestAbortion())
				foreach (var item in _driveManager.DriveList)
					_driveManager.StopTest(item);
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (GrDrives.SelectedIndex >= 0)
				_driveManager.ShowLogSelected(GrDrives.SelectedItem);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Visibility = Visibility.Hidden;
			e.Cancel = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
