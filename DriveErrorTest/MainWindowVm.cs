using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DriveErrorTest
{
	class MainWindowVm : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event Action<string, string> ErrorOccured;
		public event Action ShowWindowRequested;

		private DriveManager _driveManager;
		private DriveInfoStorage _selectedDrive;
		private SystemTrayHelper _systemTrayHelper;

		public List<TimeSpan> Spans { get; set; }

		public bool TestsRunning => _driveManager.TestsRunning;

		public DriveInfoStorage SelectedDrive
		{
			get { return _selectedDrive; }
			set
			{
				_selectedDrive = value;
				OnPropertyChanged();
			}
		}

		public DriveManager DriveManager
		{
			get { return _driveManager; }
			set
			{
				_driveManager = value;
				OnPropertyChanged();
			}
		}

		public MainWindowVm()
		{
			CommonLogger.Initialize();
			CreateSpansList();
			InitializeDriveManager();
			InitializeTrayIcon();
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

		private void InitializeTrayIcon()
		{
			_systemTrayHelper = new SystemTrayHelper();
			_systemTrayHelper.Initialize();
			_systemTrayHelper.ShowWindowEvent += SystemTrayHelper_ShowWindowEvent;
			_systemTrayHelper.ShutAppDownEvent += SystemTrayHelper_ShutAppDownEvent;
		}

		private void SystemTrayHelper_ShowWindowEvent()
		{
			ShowWindowRequested?.Invoke();
		}

		private void SystemTrayHelper_ShutAppDownEvent()
		{
			ExitApp();
		}

		public void ExitApp()
		{
			DisposeComponents();
			Application.Current.Shutdown(0);
		}

		public void TryToStartTest(object item)
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
					ErrorOccured?.Invoke(
						ex.ToString(), 
						"Не удалось запустить тестирование!" + Environment.NewLine + " Проверьте состояние устройства" + temp?.Name);
				}
			}
			else
			{
				GUIHelpers.ShowNoSourceSelectedMessage();
			}
		}

		public void StartAllTests()
		{
			foreach (var item in _driveManager.DriveList)
				TryToStartTest(item);
		}

		public void DisposeComponents()
		{
			foreach (var item in _driveManager.DriveList)
				_driveManager.StopTest(item);
			_systemTrayHelper?.Dispose();
		}

		private void InitializeDriveManager()
		{
			_driveManager = new DriveManager();
			_driveManager.Initialize();
		}

		protected void OnPropertyChanged([CallerMemberName]string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
