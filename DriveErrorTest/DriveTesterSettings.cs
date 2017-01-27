using System;
using System.IO;

namespace DriveErrorTest
{
	public class DriveTesterSettings
	{
		/// <summary>
		/// Indicates whether the drive needs to be formatted prior to starting tests
		/// </summary>
		public bool CleanStart;

		/// <summary>
		/// How often the drive is formatted and data is rewritten on it
		/// </summary>
		public TimeSpan RewritePeriod;

		/// <summary>
		/// How many recovery attempts the tester will make
		/// </summary>
		public uint RecoveryAttempts;

		/// <summary>
		/// Source data folder 
		/// </summary>
		public DirectoryInfo SourceDirectory;

		/// <summary>
		/// Path to the target log file (1 per tester) 
		/// </summary>
		public Logger Log;
	}
}
