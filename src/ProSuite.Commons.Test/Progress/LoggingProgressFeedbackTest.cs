using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.Test.Progress
{
	[TestFixture]
	public class LoggingProgressFeedbackTest
	{
		[Test]
		public void Default_constructor_logs_message_as_info_without_prefix()
		{
			var logger = new RecordingMsg();
			var feedback = new LoggingProgressFeedback(logger);

			feedback.SetRange(0, 10);
			feedback.ShowMessage("hello");

			Assert.That(logger.Calls, Has.Count.EqualTo(1));
			Assert.That(logger.Calls[0], Is.EqualTo(("Info", "hello")));
		}

		[Test]
		public void LogProgress_true_prefixes_message_with_current_and_maximum_value()
		{
			var logger = new RecordingMsg();
			var feedback = new LoggingProgressFeedback(logger, logProgress: true);

			feedback.SetRange(0, 10);
			feedback.Advance("tile 1");

			Assert.That(logger.Calls, Has.Count.EqualTo(1));
			Assert.That(logger.Calls[0], Is.EqualTo(("Info", "[1/10] tile 1")));
		}

		[TestCase(LogLevel.Debug, "Debug")]
		[TestCase(LogLevel.Info, "Info")]
		[TestCase(LogLevel.Warn, "Warn")]
		[TestCase(LogLevel.Error, "Error")]
		[TestCase(LogLevel.Fatal, "Fatal")]
		public void ShowMessage_with_level_routes_to_the_matching_logger_method(
			LogLevel level, string expectedMethod)
		{
			var logger = new RecordingMsg();
			var feedback = new LoggingProgressFeedback(logger);

			feedback.ShowMessage("message", level);

			Assert.That(logger.Calls, Has.Count.EqualTo(1));
			Assert.That(logger.Calls[0], Is.EqualTo((expectedMethod, "message")));
		}

		/// <summary>
		/// Minimal <see cref="IMsg"/> test double: records which logging method was invoked and
		/// with what (already-formatted) message, without any log4net/appender wiring.
		/// </summary>
		private sealed class RecordingMsg : MsgBase
		{
			public List<(string Method, string Message)> Calls { get; } =
				new List<(string, string)>();

			public override bool IsDebugEnabled => true;
			public override bool IsInfoEnabled => true;
			public override bool IsWarnEnabled => true;
			public override bool IsErrorEnabled => true;
			public override bool IsFatalEnabled => true;

			public override void VerboseDebugFormat(string format, params object[] args) { }
			public override void DebugFormat(string format, params object[] args) { }
			public override void InfoFormat(string format, params object[] args) { }
			public override void WarnFormat(string format, params object[] args) { }
			public override void ErrorFormat(string format, params object[] args) { }
			public override void FatalFormat(string format, params object[] args) { }

			protected override void DebugCore(string message) => Calls.Add(("Debug", message));
			protected override void InfoCore(string message) => Calls.Add(("Info", message));
			protected override void WarnCore(string message) => Calls.Add(("Warn", message));
			protected override void ErrorCore(string message) => Calls.Add(("Error", message));
			protected override void FatalCore(string message) => Calls.Add(("Fatal", message));

			protected override void DebugCore(string message, Exception exception) =>
				Calls.Add(("Debug", message));

			protected override void InfoCore(string message, Exception exception) =>
				Calls.Add(("Info", message));

			protected override void WarnCore(string message, Exception exception) =>
				Calls.Add(("Warn", message));

			protected override void ErrorCore(string message, Exception exception) =>
				Calls.Add(("Error", message));

			protected override void FatalCore(string message, Exception exception) =>
				Calls.Add(("Fatal", message));

			protected override string RenderObject(object obj) => obj?.ToString() ?? string.Empty;
		}
	}
}
