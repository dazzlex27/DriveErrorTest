using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace DriveErrorTest
{
	public enum RefreshPeriod
	{
		OneMinute = 0,
		FiveMinutes = 1,
		HalfHour = 2,
		Hour = 3,
		TwoHours = 4
	}

	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private string _sourcePath = "";
		private string _logPath = "";
		private Thread _t;
		private bool _isTesting;
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
			var drives = DriveInfo.GetDrives();

			foreach (var drive in drives.Where(drive => drive.DriveType == DriveType.Removable))
			{
				Drives.Add(drive);
				CbDrives.Items.Add(drive.Name + drive.VolumeLabel);
			}

			if (CbDrives.Items.IsEmpty)
			{
				CbDrives.Items.Add("<Съемные диски не найдены>");
				SetUIStatus(false);
				BtLaunchTesting.IsEnabled = false;
			}

			CbTimePeriod.Items.Add("Раз в 10 минут");
			CbTimePeriod.Items.Add("Раз в час");
			CbTimePeriod.Items.Add("Раз в сутки");
			CbTimePeriod.Items.Add("Раз в двое суток");
			CbTimePeriod.Items.Add("Раз в трое суток");
			CbTimePeriod.Items.Add("Раз в неделю");

			CbDrives.SelectedIndex = 0;
			CbTimePeriod.SelectedIndex = 2;
			SetStatusText("не запущено");
		}

		private void SetUIStatus(bool active)
		{
			CbTimePeriod.IsEnabled = active;
			CbDrives.IsEnabled = active;
			BtSelectSourcePath.IsEnabled = active;
			BtSelectLogPath.IsEnabled = active;
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
			return _sourcePath != "" && _logPath != "";
		}

		private void BtLaunchTesting_OnClick(object sender, RoutedEventArgs e)
		{
			if (_isTesting)
			{
				if (System.Windows.MessageBox.Show(
					"Вы действительно хотите прервать тестирование?",
					"Подтвердите действие",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					_tester.StopTest();
					TerminateTestingThread();
					BtLaunchTesting.Content = "Запустить тестирование";
					LbStatusStrip.Content = "остановлено";
					SetUIStatus(true);
					_isTesting = false;
				}
			}
			else
			{
				if (ReadyToTest())
				{
					_t = new Thread(CreateTester);
					_t.Start();
					Title = Drives[CbDrives.SelectedIndex].Name;
					SetUIStatus(false);
					BtLaunchTesting.Content = "Остановить тестирование";
					SetStatusText("запущено");
					SetUIStatus(false);
					_isTesting = true;
				}
				else
				{
					var message = "Не указаны следующие данные:";
					if (_sourcePath == "")
						message += Environment.NewLine + "Путь к папке с исходными данными";
					if (_logPath == "")
						message += Environment.NewLine + "Путь к журналу";
					System.Windows.MessageBox.Show(message, "Не хватает данных", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
		}

		private void TerminateTestingThread()
		{
			try { _t.Interrupt(); _t.Abort(); }
			catch { }
		}

		public void SetBackgroundColor(Color color)
		{
			if (Dispatcher.CheckAccess())
				Background = new SolidColorBrush(color);
			else
				Dispatcher.Invoke(new Action<Color>(SetBackgroundColor), color);
		}

		private void BtShowLog_OnClick(object sender, RoutedEventArgs e)
		{
			if (_logPath != "")
				Process.Start(_logPath);
		}

		public void SetStatusText(string message)
		{
			try
			{
				if (LbStatusStrip.Dispatcher.CheckAccess())
					LbStatusStrip.Content = message;
				else
					LbStatusStrip.Dispatcher.Invoke(new Action<string>(SetStatusText), message);
			}
			catch (Exception)
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
			_tester.StartTest();
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
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_isTesting &&
			    System.Windows.MessageBox.Show(
				    "Вы действительно хотите прервать тестирование?",
				    "Подтвердите действие",
				    MessageBoxButton.YesNo,
				    MessageBoxImage.Question) == MessageBoxResult.No)
			{
				e.Cancel = true;
				return;
			}

			_tester?.StopTest();
			TerminateTestingThread();
			Environment.Exit(0);
		}
	}
}