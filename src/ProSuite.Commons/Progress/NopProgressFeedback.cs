namespace ProSuite.Commons.Progress
{
	/// <summary>
	/// A no-op Progress Feedback implementation
	/// </summary>
	public class NopProgressFeedback : ProgressFeedbackBase
	{
		protected override void SetText(string text)
		{
			// do nothing
		}
	}
}
