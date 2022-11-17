using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing
{
	public interface IProcessingFeedback
	{
		/// <summary>
		/// Assume subsequent reports pertain to this process group
		/// </summary>
		string CurrentGroup { get; set; }

		/// <summary>
		/// Assume subsequent reports pertain to this process
		/// </summary>
		string CurrentProcess { get; set; }

		/// <summary>
		/// Assume subsequent reports pertain to this feature
		/// </summary>
		FeatureProxy CurrentFeature { get; set; }

		void ReportInfo([NotNull] string text);

		void ReportWarning([NotNull] string text, Exception exception = null);

		void ReportError([NotNull] string text, Exception exception = null);

		/// <summary>
		/// Report progress to whom it may concern.
		/// </summary>
		/// <remarks>
		/// Implementors shall interpret a percentage below 1 or above 100 as indefinite.
		/// The <paramref name="text"/> is typically something like "ProcessName: feature M of N".
		/// </remarks>
		void ReportProgress(int percentage, [CanBeNull] string text);

		/// <summary>
		/// Report that processing was stopped by the user.
		/// </summary>
		void ReportStopped();

		/// <summary>
		/// Report that processing has completed (with or without errors).
		/// </summary>
		void ReportCompleted();

		/// <summary>
		/// Return true if the user requests cancelling the process.
		/// </summary>
		/// <remarks>
		/// Processes should frequently check this property.
		/// When detecting a cancel request, the typical reaction is
		/// to throw an OperationCanceledException.
		/// </remarks>
		bool CancellationPending { get; }
	}
}
