using System;

namespace ProSuite.Commons.Callbacks
{
	public static class CallbackUtils
	{
		/// <summary>
		/// Executes the specified action only if the provided value is not null and, if it is a
		/// string, not empty.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="proc"></param>
		public static void DoWithNonNull<T>(T value, Action<T> proc) where T : class
		{
			if (value == null)
			{
				return;
			}

			if (value is string s && string.IsNullOrEmpty(s))
			{
				return;
			}

			proc(value);
		}
	}
}
