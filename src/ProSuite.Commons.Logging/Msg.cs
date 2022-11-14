using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	public sealed class Msg : Log4NetMsgBase
	{
		public Msg([CanBeNull] Type type) : base(type) { }

		/// <summary>
		/// Get the logger (aka Msg object) for the caller's class.
		/// </summary>
		[NotNull]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static IMsg ForCurrentClass()
		{
			var type = GetCallerType();
			return CreateLoggerMsgAction(type);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Type GetCallerType()
		{
			try
			{
				const int framesToSkip = 2;
				var frame = new StackFrame(framesToSkip, false);
				return frame.GetMethod().DeclaringType;
			}
			catch
			{
				return null;
			}
		}

		public static Func<Type, IMsg> CreateLoggerMsgAction { get; set; } = type => new Msg(type);
	}
}
