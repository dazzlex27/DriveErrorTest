using System;
using System.IO;

namespace DriveErrorTest
{
	public class DriveTesterSettings
	{
		/// <summary>
		/// How often the drive is formatted and data is rewritten on it
		/// </summary>
		public TimeSpan RewritePeriod { get; set; }

		/// <summary>
		/// How many recovery attempts the tester will make
		/// </summary>
		public uint RecoveryAttempts { get; set; }

		/// <summary>
		/// Source data folder 
		/// </summary>
		public DirectoryInfo SourceDirectory { get; set; }

		/// <summary>
		/// Path to the target log file (1 per tester) 
		/// </summary>
		public Logger Log { get; set; }
	}
}
