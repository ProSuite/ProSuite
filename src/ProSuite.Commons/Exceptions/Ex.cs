using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Exceptions
{
	/// <summary>
	/// Helper class for throwing exceptions.
	/// </summary>
	public static class Ex
	{
		public static void Throw<T>(string message)
			where T : Exception
		{
			var exception = (T) Activator.CreateInstance(typeof(T), message);

			throw exception;
		}

		[StringFormatMethod("format")]
		public static void Throw<T>(string format, params object[] args)
			where T : Exception
		{
			var exception = (T) Activator.CreateInstance(typeof(T),
			                                             string.Format(format, args));

			throw exception;
		}

		[StringFormatMethod("format")]
		public static void Throw<T>(Exception inner, string format, params object[] args)
			where T : Exception
		{
			var exception = (T) Activator.CreateInstance(typeof(T),
			                                             string.Format(format, args),
			                                             inner);

			throw exception;
		}
	}
}
