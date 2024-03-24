using System;

namespace ProSuite.Processing.Domain
{
	public interface IProcessingFeedback
	{
		/// <remarks>
		/// Assume subsequent reports pertain to this process group
		/// </remarks>
		string CurrentGroup { get; set; }

		/// <remarks>
		/// Assume subsequent reports pertain to this process
		/// </remarks>
		string CurrentProcess { get; set; }

		object CurrentFeature { get; set; }

		bool HadErrors { get; }

		bool HadWarnings { get; }

		void ReportInfo(string text);

		void ReportWarning(string text, Exception exception = null);

		void ReportError(string text, Exception exception = null);

		void ReportProgress(int percentage, string text = null);

		/// <returns>true iff the user requests cancelling</returns>
		/// <remarks>Processes should frequently check this property and when
		/// detecting a cancel request, throw an OperationCanceledException</remarks>
		bool CancellationPending { get; }
	}
}
