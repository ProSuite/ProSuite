using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public class LogSnapshot
	{
		public LogSnapshot(int captureCapacity, DateTime startTime,
		                   LogInspectorEntry[] capturedEvents = null,
		                   DateTime? snapshotTime = null)
		{
			CaptureCapacity = captureCapacity;
			StartTime = startTime;
			CapturedEvents = capturedEvents ?? Array.Empty<LogInspectorEntry>();
			SnapshotTime = snapshotTime ?? DateTime.Now;
		}

		public DateTime StartTime { get; }
		public DateTime SnapshotTime { get; }

		public int CaptureCapacity { get; }

		[NotNull]
		public LogInspectorEntry[] CapturedEvents { get; }

		public int ErrorCount { get; set; }
		public int DroppedErrors { get; set; }

		public int WarnCount { get; set; }
		public int DroppedWarns { get; set; }

		public int InfoCount { get; set; }
		public int DroppedInfos { get; set; }

		public int DebugCount { get; set; }
		public int DroppedDebugs { get; set; }

		public int TotalCount => ErrorCount + WarnCount + InfoCount + DebugCount;
	}
}
