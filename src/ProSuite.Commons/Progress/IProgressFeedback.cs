using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Progress
{
	public interface IProgressFeedback : IDisposable
	{
		void SetRange(int minimumValue, int maximumValue);

		void Advance();

		void Advance([NotNull] string message);

		[StringFormatMethod("format")]
		void Advance([NotNull] string format, params object[] args);

		void SetComplete();

		void SetComplete([NotNull] string message);

		[StringFormatMethod("format")]
		void SetComplete(string format, params object[] args);

		void ShowMessage([NotNull] string message, LogLevel level = LogLevel.Info);

		[StringFormatMethod("format")]
		void ShowMessage([NotNull] string format, params object[] args);

		void HideMessage();

		int CurrentValue { get; set; }
		int MinimumValue { get; set; }
		int MaximumValue { get; set; }
		int StepSize { get; set; }
	}
}
