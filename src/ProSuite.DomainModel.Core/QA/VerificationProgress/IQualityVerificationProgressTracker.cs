using System.ComponentModel;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Progress;

namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	/// <summary>
	/// Progress tracker for background quality verification that allows direct UI-binding.
	/// </summary>
	public interface IQualityVerificationProgressTracker : INotifyPropertyChanged
	{
		ServiceCallStatus RemoteCallStatus { get; set; }

		VerificationProgressType ProgressType { get; set; }

		VerificationProgressStep ProgressStep { get; set; }

		string ProcessingMessage { get; set; }

		int OverallProgressTotalSteps { get; set; }

		int OverallProgressCurrentStep { get; set; }

		int DetailedProgressTotalSteps { get; set; }

		int DetailedProgressCurrentStep { get; set; }

		EnvelopeXY CurrentTile { get; set; }

		int ErrorCount { get; set; }

		int WarningCount { get; set; }

		string StatusMessage { get; set; }

		IQualityVerificationResult QualityVerificationResult { get; set; }

		void CancelQualityVerification();
	}
}