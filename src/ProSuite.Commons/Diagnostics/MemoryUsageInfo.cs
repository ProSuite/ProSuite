using System.Diagnostics;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;

namespace ProSuite.Commons.Diagnostics
{
	public class MemoryUsageInfo
	{
		[NotNull] private readonly Process _process;

		private long _previousVirtualBytes;
		private long _previousPrivateBytes;

		public MemoryUsageInfo()
		{
			_process = Process.GetCurrentProcess();
			RotateState();
		}

		#region Properties

		public long VirtualBytes { get; private set; }

		public long VirtualBytesDelta => VirtualBytes - _previousVirtualBytes;

		public long PrivateBytes { get; private set; }

		public long PrivateBytesDelta => PrivateBytes - _previousPrivateBytes;

		#endregion

		public MemoryUsageInfo Refresh()
		{
			_process.Refresh();
			RotateState();
			return this;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("VB = {0:N0} Kb", VirtualBytes / 1024);
			AppendDelta(sb, VirtualBytesDelta / 1024);

			sb.AppendFormat(", PB = {0:N0} Kb", PrivateBytes / 1024);
			AppendDelta(sb, PrivateBytesDelta / 1024);

			return sb.ToString();
		}

		#region Non-public methods

		private static void AppendDelta(StringBuilder sb, long delta)
		{
			if (delta < 0)
			{
				sb.AppendFormat(" (-{0:N0})", -delta);
			}
			else if (delta > 0)
			{
				sb.AppendFormat(" (+{0:N0})", delta);
			}
			else
			{
				sb.Append(" (const)");
			}
		}

		private void RotateState()
		{
			long virtualBytes;
			long privateBytes;
			long workingSet;
			ProcessUtils.GetMemorySize(_process, out virtualBytes, out privateBytes,
			                           out workingSet);

			_previousVirtualBytes = VirtualBytes;
			VirtualBytes = virtualBytes;

			_previousPrivateBytes = PrivateBytes;
			PrivateBytes = privateBytes;
		}

		#endregion
	}
}