using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DriveErrorTest
{
	public class SystemTrayHelper
	{
		public event Action ShowWindowEvent;
		public event Action ShutAppDownEvent;

		private NotifyIcon _notifyIcon;
		private MenuItem _showWindowMenuItem;
		private MenuItem _shutDownMenuItem;

		public SystemTrayHelper()
		{
			_notifyIcon = new NotifyIcon
			{
				Icon = Properties.Resources.Firstfear_Whistlepuff_Usb,
				BalloonTipIcon = ToolTipIcon.Info,
				BalloonTipTitle = GlobalContext.AppTitleTextBase,
				Text = GlobalContext.AppTitleTextBase,
				Visible = true
			};

			_showWindowMenuItem = new MenuItem("Показать окно");
			_shutDownMenuItem = new MenuItem("Выйти из приложения");

			_notifyIcon.ContextMenu = new ContextMenu();
			_notifyIcon.ContextMenu.MenuItems.Add(_showWindowMenuItem);
			_notifyIcon.ContextMenu.MenuItems.Add(_shutDownMenuItem);

			_showWindowMenuItem.Click += _showWindowMenuItem_Click;
			_shutDownMenuItem.Click += _shutDownMenuItem_Click;
		}

		public void Dispose()
		{
			_showWindowMenuItem.Click -= _showWindowMenuItem_Click;
			_shutDownMenuItem.Click -= _shutDownMenuItem_Click;
			_notifyIcon.ContextMenu.MenuItems.Remove(_showWindowMenuItem);
			_notifyIcon.ContextMenu.MenuItems.Remove(_shutDownMenuItem);
			_notifyIcon.ContextMenu.Dispose();
			_showWindowMenuItem.Dispose();
			_shutDownMenuItem.Dispose();
			_notifyIcon.Dispose();
		}

		private void _shutDownMenuItem_Click(object sender, EventArgs e)
		{
			ShowWindowEvent?.Invoke();
		}

		private void _showWindowMenuItem_Click(object sender, EventArgs e)
		{
			ShutAppDownEvent?.Invoke();
		}
	}
}
