using System;
using System.ComponentModel;
using System.IO;

namespace DriveErrorTest
{
	public class DriveTesterSettings : INotifyPropertyChanged
	{
		private TimeSpan _rewritePeriod = new TimeSpan(0, 3, 0, 0);
		private DirectoryInfo _sourceDirectory;
		private bool _cleanStart;

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Indicates whether the drive needs to be formatted prior to starting tests
		/// </summary>
		public bool CleanStart
		{
			get { return _cleanStart; }
			set
			{
				_cleanStart = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CleanStart"));
			}
		}

		/// <summary>
		/// How often the drive is formatted and data is rewritten on it
		/// </summary>
		public TimeSpan RewritePeriod
		{
			get { return _rewritePeriod; }
			set
			{
				_rewritePeriod = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RewritePeriod"));
			}
		}

		/// <summary>
		/// How many recovery attempts the tester will make
		/// </summary>
		public uint RecoveryAttempts { get; set; } = 4;

		/// <summary>
		/// Source data folder 
		/// </summary>
		public DirectoryInfo SourceDirectory
		{
			get { return _sourceDirectory; }
			set
			{
				_sourceDirectory = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceDirectory"));
			}
		}

		/// <summary>
		/// Path to the target log file (1 per tester) 
		/// </summary>
		public Logger Log { get; set; }
	}
}
