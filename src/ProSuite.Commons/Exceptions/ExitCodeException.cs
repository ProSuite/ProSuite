using System;

namespace ProSuite.Commons.Exceptions
{
	/// <summary>
	/// Exception to indicate a specific exit code for the application.
	/// The main program may catch, report the message as an error, and
	/// quit the application with the given exit code.
	/// </summary>
	public class ExitCodeException : Exception
	{
		public int ExitCode { get; } = 1; // non-zero

		public ExitCodeException() { }

		/// <param name="message">the error message</param>
		/// <param name="exitCode">an optional exit code</param>
		/// <remarks>Exit code should be in the range 1 to 255;
		/// 0 would be success; other values are probably not portable</remarks>
		public ExitCodeException(string message, int exitCode = 1)
			: base(message)
		{
			ExitCode = exitCode;
		}

		public ExitCodeException(string message, Exception inner)
			: base(message, inner) { }
	}
}
