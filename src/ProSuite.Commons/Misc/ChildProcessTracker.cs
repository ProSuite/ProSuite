using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProSuite.Commons.Misc
{
	/// <summary>
	/// Implicitly creates a job object for the current process.
	/// Then all child processes registered with <see cref="AddProcess"/>
	/// will be terminated when the parent process exits (or crashes).
	/// </summary>
	/// <seealso href="https://stackoverflow.com/questions/3342941/kill-child-process-when-parent-process-is-killed"/>
	public static class ChildProcessTracker
	{
		private static readonly IntPtr _jobHandle;

		static ChildProcessTracker()
		{
			// Job objects are automatically closed (and kill their children) when
			// the last handle to them closes — which happens when this process exits,
			// for any reason, including crashes.
			_jobHandle = CreateJobObject(IntPtr.Zero, null);

			var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
			           {
				           LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
			           };

			var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			                   {
				                   BasicLimitInformation = info
			                   };

			int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
			IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);

			try
			{
				Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

				if (! SetInformationJobObject(
					    _jobHandle, JobObjectInfoType.ExtendedLimitInformation,
					    extendedInfoPtr, (uint) length))
				{
					throw new Win32Exception();
				}
			}
			finally
			{
				Marshal.FreeHGlobal(extendedInfoPtr);
			}
		}

		public static void AddProcess(Process process)
		{
			if (process is null)
			{
				return;
			}

			if (! AssignProcessToJobObject(_jobHandle, process.Handle))
			{
				// Ignore if the process has already exited:
				if (process.HasExited)
				{
					return;
				}

				throw new Win32Exception();
			}
		}

		#region P/Invoke

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetInformationJobObject(
			IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

		private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

		private enum JobObjectInfoType
		{
			ExtendedLimitInformation = 9
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			public Int64 PerProcessUserTimeLimit;
			public Int64 PerJobUserTimeLimit;
			public UInt32 LimitFlags;
			public UIntPtr MinimumWorkingSetSize;
			public UIntPtr MaximumWorkingSetSize;
			public UInt32 ActiveProcessLimit;
			public UIntPtr Affinity;
			public UInt32 PriorityClass;
			public UInt32 SchedulingClass;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
			public IO_COUNTERS IoInfo;
			public UIntPtr ProcessMemoryLimit;
			public UIntPtr JobMemoryLimit;
			public UIntPtr PeakProcessMemoryUsed;
			public UIntPtr PeakJobMemoryUsed;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IO_COUNTERS
		{
			public UInt64 ReadOperationCount;
			public UInt64 WriteOperationCount;
			public UInt64 OtherOperationCount;
			public UInt64 ReadTransferCount;
			public UInt64 WriteTransferCount;
			public UInt64 OtherTransferCount;
		}

		#endregion
	}
}
