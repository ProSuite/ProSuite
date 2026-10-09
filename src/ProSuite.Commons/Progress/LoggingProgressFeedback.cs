using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Progress
{
	public class LoggingProgressFeedback : ProgressFeedbackBase
	{
		private readonly IMsg _logger;
		private readonly bool _logProgress;

		/// <param name="logger">The logger messages are routed to.</param>
		/// <param name="logProgress">When true, messages are prefixed with
		/// <c>[CurrentValue/MaximumValue]</c> before being logged. Defaults to false, preserving
		/// this class's original (non-prefixed) behavior for existing callers.</param>
		public LoggingProgressFeedback([NotNull] IMsg logger, bool logProgress = false)
		{
			Assert.ArgumentNotNull(logger, nameof(logger));

			_logger = logger;
			_logProgress = logProgress;
		}

		#region Non-public methods

		protected override void SetText(string message)
		{
			if (! string.IsNullOrEmpty(message))
			{
				_logger.Info(Prefixed(message));
			}
		}

		protected override void SetText(string message, LogLevel level)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			string text = Prefixed(message);

			switch (level)
			{
				case LogLevel.Debug:
					_logger.Debug(text);
					break;
				case LogLevel.Warn:
					_logger.Warn(text);
					break;
				case LogLevel.Error:
					_logger.Error(text);
					break;
				case LogLevel.Fatal:
					_logger.Fatal(text);
					break;
				default:
					_logger.Info(text);
					break;
			}
		}

		private string Prefixed(string message) =>
			_logProgress ? $"[{CurrentValue}/{MaximumValue}] {message}" : message;

		#endregion
	}
}
