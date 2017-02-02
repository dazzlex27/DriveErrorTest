using System.Collections.Generic;
using System.Timers;

namespace DriveErrorTest
{
	public class TestStartQueue
	{
		private Queue<DriveInfoStorage> _driveQueue;
		private Timer _timer;

		public double StartInterval { get; private set; }

		public TestStartQueue()
		{
		}

		public void Initialize(double span)
		{
			StartInterval = span;

			_driveQueue = new Queue<DriveInfoStorage>();
			_timer = new Timer { Interval = span };
			_timer.Elapsed += TimerElapsed;
		}

		private void DequeueNext()
		{
			var nextItem = _driveQueue.Dequeue();

			if (nextItem.Running)
			{
				_driveQueue.Dequeue().StartTest();
				_timer.Start();
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			DequeueNext();
		}

		public void Add(DriveInfoStorage drive)
		{
			_driveQueue.Enqueue(drive);

			if (!_timer.Enabled)
				DequeueNext();
		}
	}
}
