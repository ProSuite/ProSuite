using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Progress
{
	public class LoggingProgressFeedback : ProgressFeedbackBase
	{
		private readonly IMsg _logger;

		public LoggingProgressFeedback([NotNull] IMsg logger)
		{
			Assert.ArgumentNotNull(logger, nameof(logger));

			_logger = logger;
		}

		#region Non-public methods

		protected override void SetText(string message)
		{
			if (! string.IsNullOrEmpty(message))
			{
				_logger.Info(message);
			}
		}

		#endregion
	}
}
