using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.System
{
	public static class ProcessUtils
	{
		[PublicAPI]
		public static void GetMemorySize(out long virtualBytes,
		                                 out long privateBytes,
		                                 out long workingSet)
		{
			using (Process process = Process.GetCurrentProcess())
			{
				GetMemorySize(process, out virtualBytes, out privateBytes, out workingSet);
			}
		}

		[PublicAPI]
		public static void GetMemorySize([NotNull] Process process,
		                                 out long virtualBytes,
		                                 out long privateBytes,
		                                 out long workingSet)
		{
			Assert.ArgumentNotNull(process, nameof(process));

			// TODO: VirtualMemorySize64 in .net core is completely different (gigantic number)
			//       Either it should not be used any more or we need to adapt VM size!
			virtualBytes = GetMemoryWorkaround(process.VirtualMemorySize64);
			privateBytes = GetMemoryWorkaround(process.PrivateMemorySize64);
			workingSet = GetMemoryWorkaround(process.WorkingSet64);
		}

		/// <summary>
		/// Starts a new process by specifying the name of a document or application file, such as an html document
		/// being shown by the browser. The associated process will be started with UseShellExecute.
		/// </summary>
		/// <param name="fileName">The full path of the document/file to be opened with the associated process.</param>
		/// <returns>The started process.</returns>
		[PublicAPI]
		public static Process StartProcess([NotNull] string fileName)
		{
			var process = new Process();

			// NOTE: In .net 6 UseShellExecute defaults to false and therefore must be set explicitly.
			process.StartInfo = new ProcessStartInfo(fileName)
			                    {
				                    UseShellExecute = true
			                    };
			process.Start();

			return process;
		}

		/// <summary>
		/// Starts a new process.
		/// </summary>
		/// <param name="fileName">The full path of the executable</param>
		/// <param name="arguments">The arguments</param>
		/// <param name="useShellExecute">Whether the process should run in a (visible) shell
		/// or completely in the background. If using false make sure to avoid dead-locks by calling 
		/// Process.BeginOutputReadLine() and Process.BeginErrorReadLine() before waiting for exit.</param>
		/// <returns></returns>
		[PublicAPI]
		public static Process StartProcess([NotNull] string fileName,
		                                   [CanBeNull] string arguments,
		                                   bool useShellExecute)
		{
			return StartProcess(fileName, arguments, useShellExecute, ! useShellExecute);
		}

		/// <summary>
		/// Starts a new process.
		/// </summary>
		/// <param name="fileName">The full path of the executable</param>
		/// <param name="arguments">The arguments</param>
		/// <param name="useShellExecute">If using false make sure to avoid dead-locks by calling 
		/// Process.BeginOutputReadLine() and Process.BeginErrorReadLine() before waiting for exit.</param>
		/// <param name="createNoWindow"></param>
		/// <param name="priorityClass">The process priority class.</param>
		/// <returns></returns>
		[NotNull]
		[PublicAPI]
		public static Process StartProcess(
			[NotNull] string fileName,
			[CanBeNull] string arguments,
			bool useShellExecute,
			bool createNoWindow,
			ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			var process = new Process();

			process.StartInfo.FileName = fileName;

			if (arguments != null)
			{
				process.StartInfo.Arguments = arguments;
			}

			process.StartInfo.UseShellExecute = useShellExecute;

			// make sure there is no dead-lock due to standard output
			process.StartInfo.RedirectStandardOutput = ! useShellExecute;
			process.StartInfo.RedirectStandardError = ! useShellExecute;

			if (createNoWindow)
			{
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.CreateNoWindow = true;
			}

			process.Start();

			process.PriorityClass = priorityClass;

			return process;
		}

		[PublicAPI]
		public static TimeSpan TryGetUserProcessorTime([CanBeNull] Process process = null)
		{
			try
			{
				if (process == null) process = Process.GetCurrentProcess();

				return process.UserProcessorTime;
			}
			catch (Win32Exception)
			{
				return TimeSpan.Zero;
			}
		}

		[PublicAPI]
		public static int GetRunningProcessCount([NotNull] string processName)
		{
			Assert.ArgumentNotNullOrEmpty(processName, nameof(processName));

			Process[] processes = Process.GetProcessesByName(processName);

			return processes.Length;
		}

		public static bool TrySetThreadIdAsName()
		{
			if (Thread.CurrentThread.Name != null)
			{
				return false;
			}

			Thread.CurrentThread.Name = $"Thread {Thread.CurrentThread.ManagedThreadId}";

			return true;
		}

		private static long GetMemoryWorkaround(long rawValue)
		{
			// in .Net 2.0, memory values can be negative due to incorrect internal casts to int

			if (rawValue < 0 && rawValue >= int.MinValue)
			{
				//Explicitly cast the value back to an unsigned 32-bit value and then widen it to 64-bits.
				//That should undo the int cast that .NET 2.0 incorrectly does as long as we're dealing with 4GB or less.
				unchecked
				{
					return (uint) rawValue;
				}
			}

			return rawValue;
		}
	}
}
