using System;
using System.Diagnostics;
using System.IO;
using log4net;
using log4net.ObjectRenderer;
using NSubstitute;
using NUnit.Framework;

namespace ProSuite.Commons.Logging.Test
{
	[TestFixture]
	public class MsgBaseTest
	{
		private void LogDebug(int count, bool usePrivateConfiguration)
		{
			LoggingConfigurator.UsePrivateConfiguration = usePrivateConfiguration;

			var msg = new Logging.Msg(GetType());

			var watch = new Stopwatch();
			watch.Start();
			for (var i = 0; i < count; i++) msg.DebugFormat("iteration {0}", i);
			watch.Stop();

			Console.Out.WriteLine(
				"{0:N0} calls to DebugFormat: {1:N0} ms",
				count, watch.ElapsedMilliseconds);
			Assert.Less(watch.ElapsedMilliseconds, (double) 100, "too slow");
		}

		private class Msg : Log4NetMsgBase
		{
			public Msg(ILog log) : base(log) { }
		}

		private class UpperCaseRenderer : IObjectRenderer
		{
			#region IObjectRenderer Members

			public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
			{
				writer.Write(obj.ToString().ToUpper());
			}

			#endregion
		}

		private class SomeLoggableClass
		{
			private readonly string _text;

			public SomeLoggableClass(string text)
			{
				_text = text;
			}

			public override string ToString()
			{
				return _text;
			}
		}

		[Test]
		[Category("fast")]
		public void AssertVerboseLoggingIsDirtCheapIfDisabled()
		{
			var realLog = LogManager.GetLogger(GetType());
			var msg = new Msg(realLog) { IsVerboseDebugEnabled = false };

			const int count = 1000000;
			var watch = new Stopwatch();
			watch.Start();
			for (var i = 0; i < count; i++)
			{
				msg.VerboseDebug(() => $"iteration {i}");
			}

			watch.Stop();

			Console.Out.WriteLine(
				"{0:N0} calls to VerboseDebugFormat while IsVerboseDebugEnabled=false: {1:N0} ms",
				count, watch.ElapsedMilliseconds);
			Assert.Less(watch.ElapsedMilliseconds, (double) 150, "too slow");
		}

		[Test]
		[Category("fast")]
		public void CanLogAtMinIndentation()
		{
			const string message = "TestMessage";

			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);

			var msg = new Msg(log);
			msg.ResetIndentation();
			msg.Debug(message);

			log.Received().Debug(message);
		}

		[Test]
		[Category("fast")]
		public void CanLogNullReference()
		{
			var realLog = LogManager.GetLogger(GetType());

			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);
			log.Logger.Returns(realLog.Logger);

			var msg = new Msg(log);
			msg.ResetIndentation();
			msg.Debug(null);

			log.Received().Debug("(null)");
		}

		[Test]
		[Category("fast")]
		public void CanLogObjectNoRenderer()
		{
			const string message = "TestMessage";

			var obj = new SomeLoggableClass(message);

			var realLog = LogManager.GetLogger(GetType());

			// expect result of SomeLoggableClass.ToString() 
			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);
			log.Logger.Returns(realLog.Logger);

			var msg = new Msg(log);
			msg.ResetIndentation();
			msg.Debug(obj);

			log.Received().Debug(message);
		}

		[Test]
		[Category("fast")]
		public void CanLogObjectWithRenderer()
		{
			const string message = "TestMessage";

			var obj = new SomeLoggableClass(message);

			var realLog = LogManager.GetLogger(GetType());
			realLog.Logger.Repository.RendererMap.Put(typeof(SomeLoggableClass),
			                                          new UpperCaseRenderer());

			// expect rendered object (SomeLoggableClass.ToString().ToUpper())
			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);
			log.Logger.Returns(realLog.Logger);

			var msg = new Msg(log);
			msg.ResetIndentation();
			msg.Debug(obj);

			log.Received().Debug(message.ToUpper());
		}

		[Test]
		[Category("fast")]
		public void CanLogToMaxIndentation()
		{
			const string message = "TestMessage";

			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);

			var msg = new Msg(log);
			msg.ResetIndentation();
			for (var indentLevel = 0;
			     indentLevel <= msg.MaximumIndentationLevel;
			     indentLevel++)
			{
				msg.Debug(message);
				msg.IncrementIndentation();
			}

			msg.Debug(message);

			log.Received().Debug(message);
			log.Received().Debug("  " + message);
			log.Received().Debug("    " + message);
			log.Received().Debug("      " + message);
			log.Received().Debug("        " + message);
			log.Received().Debug("          " + message);
			log.Received().Debug("            " + message);
			log.Received().Debug("              " + message);
			log.Received().Debug("                " + message);
			log.Received().Debug("                  " + message);
			log.Received(2).Debug("                    " + message); // expected twice
		}

		[Test]
		[Category("fast")]
		public void CanResetIndentation()
		{
			const string message = "TestMessage";

			var log = Substitute.For<ILog>();
			log.IsDebugEnabled.Returns(true);

			var msg = new Msg(log);
			msg.ResetIndentation();
			msg.Debug(message);
			msg.IncrementIndentation();
			msg.Debug(message);
			msg.ResetIndentation();
			msg.Debug(message);

			log.Received(2).Debug(message);
			log.Received().Debug("  " + message);
		}

		[Test]
		public void CanSwitchBetweenPrivateAndPublicConfiguration()
		{
			LogDebug(1, true);
			LogDebug(1, false);
			LogDebug(1, true);
			LogDebug(1, false);
		}

		[Test]
		public void NoOpLogPerformanceDefaultConfiguration()
		{
			LogDebug(100000, false);
		}

		[Test]
		public void NoOpLogPerformancePrivateConfiguration()
		{
			LogDebug(100000, true);
		}
	}
}
