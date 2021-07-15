using System;
using System.Runtime.InteropServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public static class SystemUtils
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWow64Process(IntPtr hProcess,
		                                          [MarshalAs(UnmanagedType.Bool)] out bool
			                                          isWow64);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetModuleHandle(string moduleName);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string methodName);

		public static bool Is64BitProcess
		{
			get { return IntPtr.Size == 8; }
		}

		public static bool Is64BitOperatingSystem
		{
			get
			{
				// Clearly if this is a 64-bit process we must be on a 64-bit OS.
				if (Is64BitProcess)
				{
					return true;
				}

				// Ok, so we are a 32-bit process, but is the OS 64-bit?
				// If we are running under Wow64 than the OS is 64-bit.
				bool isWow64;
				return ModuleContainsFunction("kernel32.dll", "IsWow64Process") &&
				       IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64;
			}
		}

		/// <summary>
		/// Whether the current process is large address aware in case it is a 32 bit
		/// process. This is irrelevant for 64-bit processes. To be set by the aware
		/// executable.
		/// </summary>
		public static bool IsLargeAddressAware { get; set; }

		private static bool ModuleContainsFunction([NotNull] string moduleName,
		                                           [NotNull] string methodName)
		{
			IntPtr hModule = GetModuleHandle(moduleName);

			if (hModule != IntPtr.Zero)
			{
				return GetProcAddress(hModule, methodName) != IntPtr.Zero;
			}

			return false;
		}
	}
}
