using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.WPF;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.UI.QA.VerificationProgress
{
	public class VerificationProgressViewModel : INotifyPropertyChanged
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Field declarations

		private int _warningCount;
		private int _errorCount;
		private string _runningProgressTypeText = "Initializing...";
		private string _overallProgressText;
		private Visibility _overallProgressVisible;
		private int _overallCurrentStep;
		private int _overallTotalSteps = 100;
		private EnvelopeXY _currentTile;
		private int _detailTotalSteps;
		private int _detailCurrentStep;

		private ICommand _cancelCommand;
		private ICommand _showReportCommand;
		private ICommand _saveErrorsCommand;
		private string _statusText;
		private Brush _statusTextColor = Brushes.Black;
		private string _tileInfoText;
		private Visibility _tileInfoVisible;
		private string _detailProgressText;
		private string _cancelButtonText;
		private Visibility _detailProgressVisible;

		private string _startTimeText;
		private string _endTimeText;

		#endregion

		public VerificationProgressViewModel()
		{
			CancelButtonText = "Cancel";
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#region Required properties

		/// <summary>
		/// The progress tracker that will be updated in the background and hence allows updating
		/// the UI.
		/// </summary>
		public IQualityVerificationProgressTracker ProgressTracker { get; set; }

		/// <summary>
		/// The verification action to be started when the dialog is loaded.
		/// </summary>
		public Func<Task<ServiceCallStatus>> VerificationAction { get; set; }

		/// <summary>
		/// Action to be called when the 'Show report' button is clicked.
		/// </summary>
		[CanBeNull]
		public Action<QualityVerification> ShowReportAction { get; set; }

		[CanBeNull]
		public Action<IQualityVerificationResult> SaveAction { get; set; }

		#endregion

		#region Bound properties

		public string RunningProgressTypeText
		{
			get => _runningProgressTypeText;
			set
			{
				_runningProgressTypeText = value;
				OnPropertyChanged();
			}
		}

		public string OverallProgressText
		{
			get => _overallProgressText;
			set
			{
				_overallProgressText = value;
				OnPropertyChanged();
			}
		}

		public Visibility OverallProgressVisible
		{
			get => _overallProgressVisible;
			set
			{
				_overallProgressVisible = value;
				OnPropertyChanged();
			}
		}

		public int OverallTotalSteps
		{
			get => _overallTotalSteps;
			set
			{
				_overallTotalSteps = value;
				OnPropertyChanged();
			}
		}

		public int OverallCurrentStep
		{
			get => _overallCurrentStep;
			set
			{
				_overallCurrentStep = value;
				OnPropertyChanged();
			}
		}

		public Visibility DetailProgressVisible
		{
			get => _detailProgressVisible;
			set
			{
				_detailProgressVisible = value;
				OnPropertyChanged();
			}
		}

		public string DetailProgressText
		{
			get => _detailProgressText;
			set
			{
				_detailProgressText = value;
				OnPropertyChanged();
			}
		}

		public int DetailTotalSteps
		{
			get => _detailTotalSteps;
			set
			{
				_detailTotalSteps = value;
				OnPropertyChanged();
			}
		}

		public int DetailCurrentStep
		{
			get => _detailCurrentStep;
			set
			{
				_detailCurrentStep = value;
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

				if (OverallCurrentStep <= OverallTotalSteps)
				{
					string format = _currentTile.XMax < 400
						                ? "Verifying tile {0} of {1}, (extent: {2:N4}, {3:N4}, {4:N4}, {5:N4})"
						                : "Verifying tile {0} of {1}, (extent: {2:N0}, {3:N0}, {4:N0}, {5:N0})";

					TileInfoText = string.Format(
						format,
						OverallCurrentStep, OverallTotalSteps,
						_currentTile.XMin, _currentTile.YMin,
						_currentTile.XMax, _currentTile.YMax);
				}
				else
				{
					TileInfoText = "Completed";
				}
			}
		}

		public string TileInfoText
		{
			get => _tileInfoText;
			set
			{
				_tileInfoText = value;
				OnPropertyChanged();
			}
		}

		public Visibility TileInfoVisible
		{
			get => _tileInfoVisible;
			set
			{
				_tileInfoVisible = value;
				OnPropertyChanged();
			}
		}

		public SolidColorBrush ErrorTextBoxBackColor =>
			ErrorCount > 0 ? Brushes.Red : Brushes.Transparent;

		public SolidColorBrush WarningTextBoxBackColor =>
			WarningCount > 0 ? Brushes.Yellow : Brushes.Transparent;

		public int WarningCount
		{
			get => _warningCount;
			set
			{
				_warningCount = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(WarningTextBoxBackColor)); // Or use converter?
			}
		}

		public int ErrorCount
		{
			get => _errorCount;
			set
			{
				_errorCount = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ErrorTextBoxBackColor)); // Or use converter?
			}
		}

		public string CancelButtonText
		{
			get => _cancelButtonText;
			set
			{
				_cancelButtonText = value;
				OnPropertyChanged();
			}
		}

		public string StatusText
		{
			get => _statusText;
			set
			{
				_statusText = value;
				OnPropertyChanged();
			}
		}

		public Brush StatusTextColor
		{
			get => _statusTextColor;
			set
			{
				_statusTextColor = value;
				OnPropertyChanged();
			}
		}

		public string StartTimeText
		{
			get => _startTimeText;
			set
			{
				_startTimeText = value;
				OnPropertyChanged();
			}
		}

		public string EndTimeText
		{
			get => _endTimeText;
			set
			{
				_endTimeText = value;
				OnPropertyChanged();
			}
		}

		#endregion

		/// <summary>
		/// The action that closes the host window.
		/// </summary>
		public Action CloseAction { get; set; }

		public ICommand CancelCommand
		{
			get
			{
				return _cancelCommand ??
				       (_cancelCommand =
					        new RelayCommand<VerificationProgressWpfControl>(
						        control => CancelOrClose(),
						        control => ProgressTracker != null));
			}
		}

		public ICommand SaveErrorsCommand
		{
			get
			{
				if (_saveErrorsCommand == null)
				{
					_saveErrorsCommand =
						new RelayCommand<VerificationProgressViewModel>(
							vm => SaveIssues(),
							vm =>
								CanSaveIssues());
				}

				return _saveErrorsCommand;
			}
		}

		public ICommand ShowReportCommand
		{
			get
			{
				if (_showReportCommand == null)
				{
					_showReportCommand =
						new RelayCommand<VerificationProgressViewModel>(
							vm => ShowReport(),
							vm =>
								CanShowReport());
				}

				return _showReportCommand;
			}
		}

		private IQualityVerificationResult VerificationResult =>
			ProgressTracker.QualityVerificationResult;

		public async Task<ServiceCallStatus> RunBackgroundVerificationAsync()
		{
			ServiceCallStatus result;

			Assert.NotNull(ProgressTracker, nameof(ProgressTracker));
			Assert.NotNull(VerificationAction, nameof(VerificationAction));

			try
			{
				ProgressTracker.PropertyChanged += ProgressTracker_PropertyChanged;

				DateTime startTime = DateTime.Now;

				StartTimeText = $"Processing start: {startTime:T}";

				result = await VerificationAction();

				DateTime endTime = DateTime.Now;

				EndTimeText = $"Processing end: {endTime:T}";

				TimeSpan elapsed = endTime - startTime;

				string timeSpanDisplay = $"{DateTimeUtils.Format(elapsed, true)} [HH:MM:SS]";
				if (result == ServiceCallStatus.Finished)
				{
					StatusText = $"Verification completed in {timeSpanDisplay}";
				}
				else if (result == ServiceCallStatus.Cancelled)
				{
					StatusTextColor = Brushes.Maroon;
					StatusText = $"Verification cancelled after {timeSpanDisplay}";
				}
				else
				{
					// Failed (keep the error message from the last progress)
					StatusTextColor = Brushes.Maroon;
				}
			}
			catch (Exception exception)
			{
				_msg.Debug("Error verifying quality in the background", exception);

				StatusTextColor = Brushes.Maroon;
				StatusText = $"Error: {exception.Message}";

				result = ServiceCallStatus.Failed;
			}

			RunningProgressTypeText = $"{result}";

			if (result != ServiceCallStatus.Running)
			{
				CancelButtonText = "Close";
			}

			CommandManager.InvalidateRequerySuggested();

			return result;
		}

		private void CancelOrClose()
		{
			Try(nameof(CancelOrClose),
			    () =>
			    {
				    if (ProgressTracker == null)
				    {
					    return;
				    }

				    if (ProgressTracker.RemoteCallStatus ==
				        ServiceCallStatus.Running)
				    {
					    RunningProgressTypeText = "Cancelling...";
					    ProgressTracker.CancelQualityVerification();
				    }
				    else
				    {
					    CloseAction();
				    }
			    }
			);
		}

		private bool CanSaveIssues()
		{
			bool result = false;

			Try(nameof(CanSaveIssues),
			    () =>
			    {
				    result = ProgressTracker?.RemoteCallStatus == ServiceCallStatus.Finished &&
				             SaveAction != null &&
				             VerificationResult != null &&
				             VerificationResult.CanSaveIssues;
			    });

			return result;
		}

		private void SaveIssues()
		{
			Try(nameof(SaveIssues),
			    () =>
			    {
				    SaveAction?.Invoke(VerificationResult);

				    // Once saved, the button should be disabled
				    SaveAction = null;
			    });
		}

		private void ShowReport()
		{
			Try(nameof(ShowReport),
			    () =>
			    {
				    Assert.NotNull(ShowReportAction, "ShowReportAction not set.");
				    ShowReportAction(VerificationResult.GetQualityVerification());
			    });
		}

		private bool CanShowReport()
		{
			bool result = false;

			Try(nameof(CanShowReport),
			    () =>
			    {
				    result = ShowReportAction != null &&
				             VerificationResult != null &&
				             VerificationResult.HasQualityVerification();
			    });

			return result;
		}

		private static void Try([CanBeNull] string methodName, [NotNull] Action action)
		{
			if (! string.IsNullOrEmpty(methodName))
			{
				_msg.VerboseDebugFormat("VerificationProgressViewModel.{0}", methodName);
			}

			try
			{
				action();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void ProgressTracker_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// This is a rather pedestrian implementation. Potential improvements:
			// - https://ayende.com/blog/4107/an-easier-way-to-manage-inotifypropertychanged
			// - Dependency Property
			// - UpdateControls
			switch (e.PropertyName)
			{
				case nameof(ProgressTracker.ErrorCount):
					ErrorCount = ProgressTracker.ErrorCount;
					break;
				case nameof(ProgressTracker.WarningCount):
					WarningCount = ProgressTracker.WarningCount;
					break;
				case nameof(ProgressTracker.OverallProgressTotalSteps):
					OverallTotalSteps = ProgressTracker.OverallProgressTotalSteps;
					break;
				case nameof(ProgressTracker.OverallProgressCurrentStep):
					OverallCurrentStep = ProgressTracker.OverallProgressCurrentStep;
					break;
				case nameof(ProgressTracker.DetailedProgressTotalSteps):
					DetailTotalSteps = ProgressTracker.DetailedProgressTotalSteps;
					break;
				case nameof(ProgressTracker.DetailedProgressCurrentStep):
					DetailCurrentStep = ProgressTracker.DetailedProgressCurrentStep;
					break;

				case nameof(ProgressTracker.ProgressType):
				case nameof(ProgressTracker.ProgressStep):
				case nameof(ProgressTracker.ProcessingMessage):
				case nameof(ProgressTracker.CurrentTile):
					UpdateProgressLabels(
						ProgressTracker.ProgressType,
						ProgressTracker.ProgressStep,
						ProgressTracker.ProcessingMessage,
						ProgressTracker.CurrentTile);

					if (ProgressTracker.CurrentTile != null)
					{
						CurrentTile = ProgressTracker.CurrentTile;
					}

					break;
				case nameof(ProgressTracker.StatusMessage):
					StatusText = ProgressTracker.StatusMessage;
					break;
				// ... TODO

				//default: throw new ArgumentOutOfRangeException(e.PropertyName);
			}
		}

		private void UpdateProgressLabels(VerificationProgressType progressType,
		                                  VerificationProgressStep progressStep,
		                                  [CanBeNull] string processingMessage,
		                                  [CanBeNull] EnvelopeXY currentTile)
		{
			switch (progressType)
			{
				case VerificationProgressType.PreProcess:

					RunningProgressTypeText = ! string.IsNullOrEmpty(processingMessage)
						                          ? processingMessage
						                          : "Pre-processing...";
					OverallProgressText = string.Empty;
					OverallProgressVisible = Visibility.Visible;
					break;
				case VerificationProgressType.ProcessNonCache:
					RunningProgressTypeText = "Running non-container tests...";
					OverallProgressText = "Datasets";
					OverallProgressVisible = Visibility.Visible;
					DetailProgressText = "Running test";
					DetailProgressVisible = Visibility.Visible;
					break;
				case VerificationProgressType.ProcessContainer:
					RunningProgressTypeText = "Running container tests...";
					OverallProgressText = currentTile == null ? "Standalone tests" : "Tiles";
					OverallProgressVisible = Visibility.Visible;
					TileInfoVisible = Visibility.Visible;
					DetailProgressText = processingMessage;
					break;
				// progress type Error does not change the label
			}
		}
	}
}
