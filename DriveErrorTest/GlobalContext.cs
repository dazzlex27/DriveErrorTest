using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DriveErrorTest
{
	public static class GlobalContext
	{
		public static string AppTitleTextBase = "FDT v" + Assembly.GetExecutingAssembly().GetName().Version;
	}
}
