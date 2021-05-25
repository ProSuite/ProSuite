using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	/// <summary>
	/// A facility to add context to logging events, using the ThreadContext
	/// feature of log4net. Logging context is an object that implements
	/// <see cref="ILoggingContext"/>. Use the set methods to establish
	/// a logging context. Use <see cref="GetLoggingContext"/> to get the
	/// logging context (if any), for a given logging event.
	/// </summary>
	public static class LoggingContext
	{
		private const string ContextPropertyName = "ProSuite.Commons.Logging:Context";

		[CanBeNull]
		public static ILoggingContext GetLoggingContext(LoggingEvent loggingEvent)
		{
			var property = loggingEvent.LookupProperty(ContextPropertyName);

			return property as ILoggingContext;
		}

		public static void SetLoggingContext(ILoggingContext context)
		{
			ThreadContext.Properties[ContextPropertyName] = context;
		}

		public static void SetLoggingContext(params object[] contextStack)
		{
			SetLoggingContext(DefaultLoggingContext.Create(contextStack));
		}

		#region Nested type

		private class DefaultLoggingContext : ILoggingContext
		{
			private readonly object[] _contextStack;

			private DefaultLoggingContext(object[] contextStack)
			{
				_contextStack = contextStack ?? throw new ArgumentNullException();
				TopMessage = GetTopMessage(contextStack);
			}

			public static DefaultLoggingContext Create(params object[] contextStack)
			{
				if (contextStack == null) return null;
				return new DefaultLoggingContext(contextStack);
			}

			public string TopMessage { get; }

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public IEnumerator<object> GetEnumerator()
			{
				return _contextStack.Cast<object>().GetEnumerator();
			}

			private static string GetTopMessage(object[] contextStack)
			{
				if (contextStack == null || contextStack.Length < 1) return null;
				var top = contextStack[contextStack.Length - 1];
				return Convert.ToString(top);
			}
		}

		#endregion
	}
}
