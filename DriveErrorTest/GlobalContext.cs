using System.Reflection;

namespace DriveErrorTest
{
	public static class GlobalContext
	{
		public static string AppTitleTextBase = "FDT v" +
		                                        Assembly.GetExecutingAssembly()
			                                        .GetName()
			                                        .Version.ToString()
			                                        .Replace(".0.0", "");
	}
}
