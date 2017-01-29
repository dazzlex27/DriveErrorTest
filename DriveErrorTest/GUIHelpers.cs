using System;
using System.ComponentModel;
using System.Reflection;
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

		public static string GetPropertyDisplayName(object descriptor)
		{
			var pd = descriptor as PropertyDescriptor;

			if (pd != null)
			{
				// Check for DisplayName attribute and set the column header accordingly
				var displayName = pd.Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;

				if (displayName != null && displayName != DisplayNameAttribute.Default)
				{
					return displayName.DisplayName;
				}

			}
			else
			{
				var pi = descriptor as PropertyInfo;

				if (pi != null)
				{
					// Check for DisplayName attribute and set the column header accordingly
					Object[] attributes = pi.GetCustomAttributes(typeof(DisplayNameAttribute), true);
					for (int i = 0; i < attributes.Length; ++i)
					{
						var displayName = attributes[i] as DisplayNameAttribute;
						if (displayName != null && displayName != DisplayNameAttribute.Default)
						{
							return displayName.DisplayName;
						}
					}
				}
			}

			return null;
		}
	}
}
