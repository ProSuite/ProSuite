using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Progress;

namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	public class QualityVerificationProgressTracker : IQualityVerificationProgressTracker
	{
		private ServiceCallStatus _serviceCallStatus;

		private VerificationProgressType _progressType;
		private VerificationProgressStep _progressStep;

		private string _processingMessage;

		private int _errorCount;
		private int _warningCount;
		private EnvelopeXY _currentTile;

		private int _overallProgressTotalSteps;
		private int _overallProgressCurrentStep;

		private int _detailedProgressTotalSteps;
		private int _detailedProgressCurrentStep;

		private string _statusMessage;

		public event PropertyChangedEventHandler PropertyChanged;

		public ServiceCallStatus RemoteCallStatus
		{
			get => _serviceCallStatus;
			set
			{
				_serviceCallStatus = value;
				OnPropertyChanged();
			}
		}

		public VerificationProgressType ProgressType
		{
			get => _progressType;
			set
			{
				_progressType = value;
				OnPropertyChanged();
			}
		}

		public VerificationProgressStep ProgressStep
		{
			get => _progressStep;
			set
			{
				_progressStep = value;
				OnPropertyChanged();
			}
		}

		public string ProcessingMessage
		{
			get => _processingMessage;
			set
			{
				_processingMessage = value;
				OnPropertyChanged();
			}
		}

		public int OverallProgressTotalSteps
		{
			get => _overallProgressTotalSteps;
			set
			{
				_overallProgressTotalSteps = value;
				OnPropertyChanged();
			}
		}

		public int OverallProgressCurrentStep
		{
			get => _overallProgressCurrentStep;
			set
			{
				_overallProgressCurrentStep = value;
				OnPropertyChanged(nameof(OverallProgressCurrentStep));
			}
		}

		public int DetailedProgressTotalSteps
		{
			get => _detailedProgressTotalSteps;
			set
			{
				_detailedProgressTotalSteps = value;
				OnPropertyChanged();
			}
		}

		public int DetailedProgressCurrentStep
		{
			get => _detailedProgressCurrentStep;
			set
			{
				_detailedProgressCurrentStep = value;
				OnPropertyChanged();
			}
		}

		public EnvelopeXY CurrentTile
		{
			get => _currentTile;
			set
			{
				_currentTile = value;
				OnPropertyChanged();
			}
		}

		public IQualityVerificationResult QualityVerificationResult { get; set; }

		public int ErrorCount
		{
			get => _errorCount;
			set
			{
				_errorCount = value;
				OnPropertyChanged();
			}
		}

		public int WarningCount
		{
			get => _warningCount;
			set
			{
				_warningCount = value;
				OnPropertyChanged();
			}
		}

		public string StatusMessage
		{
			get => _statusMessage;
			set
			{
				_statusMessage = value;
				OnPropertyChanged();
			}
		}

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public void CancelQualityVerification()
		{
			CancellationTokenSource?.Cancel();
		}

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
