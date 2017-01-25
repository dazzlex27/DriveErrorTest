using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

namespace DriveErrorTest
{
	public static class GUIHelpers
	{
		public static void SetLabelText(Window window, Label label, string message)
		{
			try
			{
				if (window.Dispatcher.CheckAccess())
				{
					if (label.Dispatcher.CheckAccess())
						label.Content = message;
					else
						label.Dispatcher.Invoke(new Action<Window, Label, string>(SetLabelText), window, label, message);
				}
				else
					window.Dispatcher.Invoke(new Action<Window, Label, string>(SetLabelText), window, label, message);
			}
			catch (Exception)
			{
			}
		}

		public static void SetWindowTaskbarStatus(Window window, TaskbarItemProgressState state, double value)
		{
			try
			{
				if (window.Dispatcher.CheckAccess())
				{
					if (window.TaskbarItemInfo.Dispatcher.CheckAccess())
					{
						window.TaskbarItemInfo.ProgressState = state;
						window.TaskbarItemInfo.ProgressValue = value;
					}
					else
						window.TaskbarItemInfo.Dispatcher.Invoke(
							new Action<Window, TaskbarItemProgressState, double>(SetWindowTaskbarStatus), window, state, value);
				}
				else
					window.Dispatcher.Invoke(
						new Action<Window, TaskbarItemProgressState, double>(SetWindowTaskbarStatus), window, state, value);
			}
			catch (Exception)
			{
			}
		}

		public static void SetWindowBackgroundColor(Window window, Color color)
		{
			try
			{
				if (window.Dispatcher.CheckAccess())
					window.Background = new SolidColorBrush(color);
				else
					window.Dispatcher.Invoke(new Action<Window, Color>(SetWindowBackgroundColor), window, color);
			}
			catch (Exception)
			{
			}
		}
	}
}
