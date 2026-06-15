using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.WPF;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.UI.Core.QA.VerificationProgress
{
	public class VerificationProgressViewModel : INotifyPropertyChanged
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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
		private RelayCommand<VerificationProgressViewModel> _saveErrorsCommand;
		private string _statusText;
		private Brush _statusTextColor = Brushes.Black;
		private string _tileInfoText;
		private Visibility _tileInfoVisible;
		private string _detailProgressText;
		private string _cancelButtonText;
		private Visibility _detailProgressVisible;

		private string _startTimeText;
		private string _endTimeText;
		private SolidColorBrush _runningProgressBackColor;

		private readonly List<EnvelopeXY> _allTiles = new List<EnvelopeXY>();

		private RelayCommand<VerificationProgressViewModel> _flashProgressCmd;
		private bool _issueOptionsEnabled;
		private string _showReportToolTip;
		private string _saveErrorsToolTip;
		private string _flashProgressToolTip;
		private ICommand _zoomToPerimeterCommand;
		private string _zoomToVerifiedPerimeterToolTip;
		private RelayCommand<VerificationProgressViewModel> _openWorkListCommand;
		private string _openWorkListToolTip;

		private readonly Latch _latch = new Latch();

		#endregion

		public VerificationProgressViewModel()
		{
			CancelButtonText = "Cancel";
			DetailProgressVisible = Visibility.Hidden;
			OverallProgressVisible = Visibility.Hidden;

			UpdateOptions = new UpdateIssuesOptionsViewModel();
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
		/// The gateway into application-specific actions, such as flashing the current progress,
		/// saving or showing the report.
		/// </summary>
		public IApplicationBackgroundVerificationController ApplicationController { get; set; }

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

		public SolidColorBrush RunningProgressBackColor
		{
			get => _runningProgressBackColor;
			set
			{
				_runningProgressBackColor = value;
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
				if (value != null && value.Equals(_currentTile))
				{
					return;
				}

				_allTiles.Add(value);

				_currentTile = value;
				OnPropertyChanged();

				if (OverallCurrentStep <= OverallTotalSteps)
				{
					Assert.NotNull(_currentTile);

					//TODO TileInfoText could be improved
					//  maybe add TileSize or display Width+Height of the current tile
					//  maybe use EnvelopeXY.ToString(CultureInfo.CurrentCulture)
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

		/// <summary>
		/// Whether the save option to keep previous issues in the work list is disabled or not.
		/// The default is enabled (false).
		/// </summary>
		public bool KeepPreviousIssuesDisabled
		{
			get => UpdateOptions.KeepPreviousIssuesEnabled;
			set => UpdateOptions.KeepPreviousIssuesEnabled = ! value;
		}

		public UpdateIssuesOptionsViewModel UpdateOptions { get; }

		#endregion

		#region Buttons

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
							vm => CanSaveIssues());
				}

				return _saveErrorsCommand;
			}
		}

		public bool IssueOptionsEnabled
		{
			get { return _issueOptionsEnabled; }
			set
			{
				_issueOptionsEnabled = value;
				OnPropertyChanged();
			}
		}

		public string SaveErrorsToolTip
		{
			get
			{
				if (string.IsNullOrEmpty(_saveErrorsToolTip))
				{
					ApplicationController?.CanSaveIssues(VerificationResult,
					                                     out _saveErrorsToolTip);
				}

				return _saveErrorsToolTip;
			}
			set
			{
				_saveErrorsToolTip = value;
				OnPropertyChanged();
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
							vm => CanShowReport());
				}

				return _showReportCommand;
			}
		}

		public string ShowReportToolTip
		{
			get
			{
				if (string.IsNullOrEmpty(_showReportToolTip))
				{
					ApplicationController?.CanShowReport(ProgressTracker.RemoteCallStatus,
					                                     VerificationResult,
					                                     out _showReportToolTip);
				}

				return _showReportToolTip;
			}
			set
			{
				_showReportToolTip = value;
				OnPropertyChanged();
			}
		}

		public ICommand OpenWorkListCommand
		{
			get
			{
				if (_openWorkListCommand == null)
				{
					_openWorkListCommand = new RelayCommand<VerificationProgressViewModel>(
						vm => _ = OpenWorkList(),
						vm => CanOpenWorkList());
				}

				return _openWorkListCommand;
			}
		}

		public string OpenWorkListToolTip
		{
			get => _openWorkListToolTip;
			set
			{
				_openWorkListToolTip = value;
				OnPropertyChanged();
			}
		}

		public ICommand ZoomToPerimeterCommand
		{
			get
			{
				if (_zoomToPerimeterCommand == null)
				{
					_zoomToPerimeterCommand = new RelayCommand<VerificationProgressViewModel>(
						vm => ApplicationController.ZoomToVerifiedPerimeter(),
						vm => CanZoomToPerimeter());
				}

				return _zoomToPerimeterCommand;
			}
		}

		public string ZoomToVerifiedPerimeterToolTip
		{
			get => _zoomToVerifiedPerimeterToolTip;
			set
			{
				_zoomToVerifiedPerimeterToolTip = value;
				OnPropertyChanged();
			}
		}

		public ICommand FlashProgressCommand
		{
			get
			{
				if (_flashProgressCmd == null)
				{
					_flashProgressCmd = new RelayCommand<VerificationProgressViewModel>(
						FlashProgress, (vm) => CanFlash());
				}

				return _flashProgressCmd;
			}
		}

		public string FlashProgressToolTip
		{
			get => _flashProgressToolTip;
			set
			{
				_flashProgressToolTip = value;
				OnPropertyChanged();
			}
		}

		#endregion

		[CanBeNull]
		private IQualityVerificationResult VerificationResult =>
			ProgressTracker?.QualityVerificationResult;

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

				// Otherwise the CancelOrClose button thinks it is still running and tries to cancel...
				ProgressTracker.RemoteCallStatus = ServiceCallStatus.Failed;
			}

			RunningProgressTypeText = $"{result}";

			switch (result)
			{
				case ServiceCallStatus.Failed:
					RunningProgressBackColor = Brushes.OrangeRed;
					break;
				case ServiceCallStatus.Cancelled:
					RunningProgressBackColor = Brushes.SandyBrown;
					break;
				case ServiceCallStatus.Finished:
					RunningProgressBackColor = Brushes.PaleGreen;
					break;
			}

			if (result != ServiceCallStatus.Running)
			{
				CancelButtonText = "Close";
			}

			CommandManager.InvalidateRequerySuggested();

			if (VerificationResult?.HasIssues == true &&
			    EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				    "PROSUITE_AUTO_OPEN_ISSUE_WORKLIST"))
			{
				await ViewUtils.RunOnUIThread(async () =>
				{
					try
					{
						IQualityVerificationResult verificationResult =
							Assert.NotNull(VerificationResult);

						await ApplicationController.OpenWorkList(
							verificationResult, replaceExisting: true);
					}
					catch (Exception ex)
					{
						ErrorHandler.HandleError(ex, _msg);
					}
				});
			}

			return result;
		}

		public void Closing(object sender, CancelEventArgs e)
		{
			// Just in case the dialog is closed by the eXit button:
			if (ProgressTracker == null)
			{
				return;
			}

			if (ProgressTracker.RemoteCallStatus ==
			    ServiceCallStatus.Running)
			{
				ProgressTracker.CancelQualityVerification();
			}
		}

		private bool CanFlash()
		{
			string reason = null;
			bool canFlash = ApplicationController?.CanFlashProgress(
				                ProgressTracker.RemoteCallStatus, _allTiles,
				                out reason) == true;

			FlashProgressToolTip = reason ?? "Show the tile verification progress";

			return canFlash;
		}

		private void FlashProgress(VerificationProgressViewModel viewModel)
		{
			Try(nameof(FlashProgress),
			    () =>
			    {
				    ApplicationController?.FlashProgress(
					    _allTiles, ProgressTracker.RemoteCallStatus);
			    });
		}

		private bool CanZoomToPerimeter()
		{
			string reason = null;

			bool result = ApplicationController?.CanZoomToVerifiedPerimeter(out reason) ?? false;

			ZoomToVerifiedPerimeterToolTip = reason ?? "Zoom to the verification perimeter";

			return result;
		}

		private bool CanOpenWorkList()
		{
			string reason = null;
			bool result = false;
			try
			{
				result =
					ApplicationController?.CanOpenWorkList(ProgressTracker.RemoteCallStatus,
					                                       VerificationResult, out reason) ?? false;

				OpenWorkListToolTip = reason ?? "Open Issue Work List";
			}
			catch (Exception e)
			{
				_msg.Error(e);
			}

			return result;
		}

		private async Task OpenWorkList()
		{
			try
			{
				await ApplicationController.OpenWorkList(
					Assert.NotNull(VerificationResult), false);
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
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
				    // TOP-5369: Failed runs should also be saveable
				    bool saveableStatus =
					    ProgressTracker?.RemoteCallStatus == ServiceCallStatus.Finished ||
					    ProgressTracker?.RemoteCallStatus == ServiceCallStatus.Failed;

				    if (! saveableStatus)
				    {
					    SaveErrorsToolTip = "Save found issues once the verification is finished";
					    result = false;
				    }

				    if (_latch.IsLatched)
				    {
					    SaveErrorsToolTip = "Issues are already being saved";
					    result = false;
				    }
				    else
				    {
					    string reason = null;
					    result = VerificationResult != null &&
					             ApplicationController != null &&
					             ApplicationController.CanSaveIssues(
						             VerificationResult, out reason);

					    SaveErrorsToolTip = reason;
				    }

				    IssueOptionsEnabled = result;
			    });

			return result;
		}

		private async void SaveIssues()
		{
			{
				_msg.VerboseDebug(() => $"VerificationProgressViewModel.{nameof(SaveIssues)}");
			}

			try
			{
				if (ApplicationController == null)
				{
					return;
				}

				if (_latch.IsLatched)
				{
					_msg.Debug("SaveIssues already running, returning");
					return;
				}

				try
				{
					_latch.Increment();

					Mouse.OverrideCursor = Cursors.Wait;

					await ApplicationController.SaveIssuesAsync(Assert.NotNull(VerificationResult),
					                                            UpdateOptions.ErrorDeletionType,
					                                            ! UpdateOptions.KeepPreviousIssues);

					_saveErrorsCommand?.RaiseCanExecuteChanged();
					_openWorkListCommand?.RaiseCanExecuteChanged();
				}
				finally
				{
					_latch.Decrement();
					Mouse.OverrideCursor = null;
				}
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		private void ShowReport()
		{
			Try(nameof(ShowReport),
			    () => { ApplicationController?.ShowReport(Assert.NotNull(VerificationResult)); });
		}

		private bool CanShowReport()
		{
			bool result = false;

			Try(nameof(CanShowReport),
			    () =>
			    {
				    string reason = null;

				    result = ApplicationController != null &&
				             VerificationResult != null &&
				             ApplicationController.CanShowReport(
					             ProgressTracker.RemoteCallStatus, VerificationResult, out reason);

				    ShowReportToolTip = reason;
			    });

			return result;
		}

		private static void Try([CanBeNull] string methodName, [NotNull] Action action)
		{
			if (! string.IsNullOrEmpty(methodName))
			{
				_msg.VerboseDebug(() => $"VerificationProgressViewModel.{methodName}");
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

						// Trigger the flash command's CanExecuteChanged:
						_flashProgressCmd?.RaiseCanExecuteChanged();

						//Application.Current.Dispatcher.BeginInvoke(
						//	new Action(delegate
						//	{
						//		CommandManager.InvalidateRequerySuggested();
						//	}));
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
				case VerificationProgressType.ProcessParallel:
					RunningProgressTypeText = "Parallel test execution...";
					OverallProgressText = currentTile == null ? "Standalone tests" : "Tiles";
					OverallProgressVisible = Visibility.Visible;
					DetailProgressText = processingMessage;
					break;
				// progress type Error does not change the label
			}
		}
	}
}
