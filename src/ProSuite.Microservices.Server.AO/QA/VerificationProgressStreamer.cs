using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using Grpc.Core;
using log4net.Core;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class VerificationProgressStreamer<T> : IVerificationProgressStreamer where T : class
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private DateTime _lastProgressTime = DateTime.MinValue;

		private readonly List<esriGeometryType> _standaloneIssueGeometryTypes =
			new List<esriGeometryType>
			{
				esriGeometryType.esriGeometryNull,
				esriGeometryType.esriGeometryMultipoint,
				esriGeometryType.esriGeometryPolyline,
				esriGeometryType.esriGeometryPolygon
			};

		private readonly IServerStreamWriter<T> _responseStream;

		[NotNull] private readonly VerificationProgressMsg _currentProgressMsg;

		[NotNull] private readonly ConcurrentBag<IssueMsg> _pendingIssues =
			new ConcurrentBag<IssueMsg>();

		/// <summary>
		/// Initializes a new instance of the <see cref="VerificationProgressStreamer{T}"/> class.
		/// </summary>
		/// <param name="responseStream">The response stream to write to</param>
		public VerificationProgressStreamer([NotNull] IServerStreamWriter<T> responseStream)
		{
			_responseStream = responseStream;
			_currentProgressMsg = new VerificationProgressMsg();
		}

		/// <summary>
		/// Sets the background verification input object, that is used to get the issues' spatial
		/// reference and the supported geometry types. Either this or
		/// <see cref="KnownIssueSpatialReference"/> must be specified.
		/// </summary>
		[CanBeNull]
		public IBackgroundVerificationInputs BackgroundVerificationInputs { get; set; }

		/// <summary>
		/// Set the known issue spatial reference if <see cref="BackgroundVerificationInputs"/> has
		/// not been set.
		/// </summary>
		[CanBeNull]
		public ISpatialReference KnownIssueSpatialReference { get; set; }

		public Func<ServiceCallStatus, VerificationProgressMsg, IList<IssueMsg>, T>
			CreateResponseAction { get; set; }

		/// <summary>
		/// Interim property that allows remembering the last log message level.
		/// Can be removed once the StandAloneVerification reports proper Progress Events.
		/// </summary>
		public int CurrentLogLevel { get; set; }

		public VerificationResponse CreateVerificationResponse(
			ServiceCallStatus callStatus,
			[NotNull] VerificationProgressMsg progressMsg,
			[NotNull] IList<IssueMsg> issues)
		{
			var response = new VerificationResponse
			               {
				               ServiceCallStatus = (int) callStatus,
				               Progress = progressMsg
			               };

			foreach (IssueMsg issueMsg in issues)
			{
				response.Issues.Add(issueMsg);
			}

			return response;
		}

		public DataVerificationResponse CreateDataVerificationResponse(
			ServiceCallStatus callStatus,
			[NotNull] VerificationProgressMsg progressMsg,
			[NotNull] IList<IssueMsg> issues)
		{
			var basicResponse = CreateVerificationResponse(callStatus, progressMsg, issues);

			return new DataVerificationResponse()
			       {
				       Response = basicResponse
			       };
		}

		public StandaloneVerificationResponse CreateStandaloneResponse(
			ServiceCallStatus callStatus,
			[NotNull] VerificationProgressMsg progressMsg,
			[NotNull] IList<IssueMsg> issues)
		{
			var response = new StandaloneVerificationResponse

			               {
				               ServiceCallStatus = (int) callStatus
			               };

			response.Message = new LogMsg()
			                   {
				                   Message = progressMsg.Message,
				                   MessageLevel = CurrentLogLevel
			                   };

			foreach (IssueMsg issueMsg in issues)
			{
				response.Issues.Add(issueMsg);
			}

			return response;
		}

		public void Info(string text)
		{
			int messageLevel = Level.Info.Value;

			_msg.Info(text);

			WriteMessage(text, messageLevel);
		}

		public void Warning(string text)
		{
			int messageLevel = Level.Warn.Value;

			_msg.Warn(text);

			WriteMessage(text, messageLevel);
		}

		public void WriteProgressAndIssues(
			VerificationProgressEventArgs progressEvent,
			ServiceCallStatus callStatus)
		{
			if (! IsPriorityProgress(progressEvent, _currentProgressMsg, _pendingIssues) &&
			    DateTime.Now - _lastProgressTime < TimeSpan.FromSeconds(1))
			{
				return;
			}

			_lastProgressTime = DateTime.Now;

			if (! UpdateProgress(_currentProgressMsg, progressEvent) && _pendingIssues.Count == 0)
			{
				return;
			}

			IList<IssueMsg> issuesToSend = DeQueuePendingIssues();

			T response = CreateResponseAction(callStatus, _currentProgressMsg, issuesToSend);

			if (issuesToSend.Count > 0)
			{
				_msg.DebugFormat("Sending {0} errors back to client...", issuesToSend.Count);
			}

			try
			{
				_responseStream.WriteAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.VerboseDebug(() => "Error sending progress to the client", ex);

				// The issues would be lost, so put them back into the collection
				foreach (IssueMsg issue in issuesToSend)
				{
					_pendingIssues.Add(issue);
				}
			}
		}

		public ServiceCallStatus SendFinalResponse(
			[CanBeNull] QualityVerification verification,
			[CanBeNull] string qaServiceCancellationMessage,
			IEnumerable<GdbObjRefMsg> deletableAllowedErrors,
			[CanBeNull] IEnvelope verifiedPerimeter,
			[CanBeNull] ITrackCancel cancelTracker)
		{
			IList<IssueMsg> issuesToSend = DeQueuePendingIssues();

			ServiceCallStatus finalStatus =
				GetFinalCallStatus(verification, qaServiceCancellationMessage);

			var finalProgress = new VerificationProgressMsg();

			if (! string.IsNullOrEmpty(qaServiceCancellationMessage))
			{
				finalProgress.Message = qaServiceCancellationMessage;
			}

			// Ensure that progress is at 100%:
			finalProgress.OverallProgressCurrentStep = 10;
			finalProgress.OverallProgressTotalSteps = 10;
			finalProgress.DetailedProgressCurrentStep = 10;
			finalProgress.DetailedProgressTotalSteps = 10;

			T response = CreateResponseAction(finalStatus, finalProgress, issuesToSend);

			if (TryGetVerificationResponse(response, out VerificationResponse verificationResponse))
			{
				// It's a DDX-based verification, pack extra results
				QualityVerificationMsg verificationMsg = PackVerification(verification);

				if (verificationMsg != null)
				{
					verificationResponse.QualityVerification = verificationMsg;
				}

				verificationResponse.ObsoleteExceptions.AddRange(deletableAllowedErrors);

				if (verifiedPerimeter != null)
				{
					verificationResponse.VerifiedPerimeter =
						ProtobufGeometryUtils.ToShapeMsg(verifiedPerimeter);
				}
			}

			_msg.DebugFormat(
				"Sending final message with {0} errors back to client...", issuesToSend.Count);

			try
			{
				_responseStream.WriteAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				if (finalStatus == ServiceCallStatus.Cancelled ||
				    cancelTracker?.Continue() == false ||
				    ex.Message == "Already finished.")
				{
					// Typically: System.InvalidOperationException: Already finished.
					_msg.Debug(
						"The verification has been cancelled and the client is already gone.", ex);

					return finalStatus;
				}

				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.Warn(
					"Error sending progress to the client. Retrying the last response in 1s...",
					ex);

				// Re-try (only for final message)
				Task.Delay(1000).Wait();
				_responseStream.WriteAsync(response);
			}

			return finalStatus;
		}

		private void WriteMessage(string text, int messageLevel)
		{
			IList<IssueMsg> issuesToSend = DeQueuePendingIssues();

			_currentProgressMsg.Message = text;
			_currentProgressMsg.MessageLevel = messageLevel;

			try
			{
				T response =
					CreateResponseAction(ServiceCallStatus.Running, _currentProgressMsg,
					                     issuesToSend);

				_msg.VerboseDebug(() => $"Sending message with {issuesToSend.Count} " +
				                        "errors back to client...");

				_responseStream.WriteAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.VerboseDebug(() => "Error sending progress to the client", ex);

				// The issues would be lost, so put them back into the collection
				foreach (IssueMsg issue in issuesToSend)
				{
					_pendingIssues.Add(issue);
				}
			}
			finally
			{
				_currentProgressMsg.MessageLevel = 0;
				_currentProgressMsg.Message = string.Empty;
			}
		}

		private static bool IsPriorityProgress(VerificationProgressEventArgs args,
		                                       VerificationProgressMsg currentProgress,
		                                       ConcurrentBag<IssueMsg> issueCollection)
		{
			if (args.ProgressType == VerificationProgressType.Undefined)
			{
				// StandAlone: Interim progress type until StandaloneVerification also reports real progress:
				return true;
			}

			if (args.ProgressType == VerificationProgressType.Error && issueCollection.Count < 10)
			{
				return false;
			}

			if (args.ProgressType == VerificationProgressType.ProcessParallel)
			{
				// TODO: Work out better overall progress steps
				return true;
			}

			if (currentProgress.ProgressType != (int) args.ProgressType)
			{
				return true;
			}

			if (! IsRelevantStep(args.ProgressStep))
			{
				return false;
			}

			VerificationProgressStep progressStep = ToVerificationStep(args.ProgressStep);

			return currentProgress.ProgressStep != (int) progressStep;
		}

		private static bool IsRelevantStep(Step progressStep)
		{
			return ToVerificationStep(progressStep) != VerificationProgressStep.Undefined;
		}

		private IList<IssueMsg> DeQueuePendingIssues()
		{
			IList<IssueMsg> issuesToSend = new List<IssueMsg>(_pendingIssues.Count);

			while (_pendingIssues.TryTake(out IssueMsg issue))
			{
				issuesToSend.Add(issue);
			}

			return issuesToSend;
		}

		#region Progress updating

		private static bool UpdateProgress(VerificationProgressMsg currentProgress,
		                                   VerificationProgressEventArgs e)
		{
			if (e.ProgressType == VerificationProgressType.PreProcess)
			{
				currentProgress.ProcessingStepMessage = e.Tag as string ?? string.Empty;
				SetOverallStep(currentProgress, e);
			}
			else if (e.ProgressType == VerificationProgressType.ProcessNonCache)
			{
				UpdateNonContainerProgress(currentProgress, e);
			}
			else if (e.ProgressType == VerificationProgressType.ProcessContainer)
			{
				if (! UpdateContainerProgress(currentProgress, e))
				{
					return false;
				}
			}
			else if (e.ProgressType == VerificationProgressType.ProcessParallel)
			{
				if (! UpdateParallelProgress(currentProgress, e))
				{
					return false;
				}
			}
			else if (e.ProgressType == VerificationProgressType.Undefined)
			{
				// Interim hack until StandaloneVerification also reports real progress:
				currentProgress.Message = e.Tag?.ToString() ?? string.Empty;
			}

			currentProgress.ProgressType = (int) e.ProgressType;

			return true;
		}

		private static bool UpdateParallelProgress(VerificationProgressMsg currentProgress,
		                                           VerificationProgressEventArgs e)
		{
			SetOverallStep(currentProgress, e);

			if (e.CurrentBox != null)
			{
				currentProgress.CurrentBox =
					ProtobufGeometryUtils.ToEnvelopeMsg(e.CurrentBox);
			}

			return true;
		}

		private static bool UpdateContainerProgress(VerificationProgressMsg currentProgress,
		                                            VerificationProgressEventArgs e)
		{
			VerificationProgressStep newProgressStep = ToVerificationStep(e.ProgressStep);

			switch (newProgressStep)
			{
				case VerificationProgressStep.TileProcessing:
					// New tile:
					SetOverallStep(currentProgress, e);
					ResetDetailStep(currentProgress);
					currentProgress.CurrentBox =
						ProtobufGeometryUtils.ToEnvelopeMsg(e.CurrentBox);
					break;
				case VerificationProgressStep.DataLoading:
					//SetOverallStep(currentProgress, e);
					SetDetailStep(currentProgress, e);
					currentProgress.ProcessingStepMessage = "Loading data";
					currentProgress.Message = ((IReadOnlyDataset) e.Tag).Name;
					break;

				case VerificationProgressStep.Testing:

					if (currentProgress.ProgressStep != (int) newProgressStep)
					{
						// First time
						ResetDetailStep(currentProgress);
						currentProgress.ProcessingStepMessage = "Testing rows";
					}

					double relativeProgress =
						((double) e.Current - currentProgress.DetailedProgressCurrentStep) /
						e.Total;

					if (relativeProgress > 0.05)
					{
						SetDetailStep(currentProgress, e);
						var testRow = e.Tag as TestRow;
						currentProgress.Message = testRow?.DataReference.DatasetName;
					}
					else
					{
						return false;
					}

					break;
				case VerificationProgressStep.TileCompleting:
					SetDetailStep(currentProgress, e);
					currentProgress.ProcessingStepMessage = "Completing tile";
					currentProgress.Message = ((QualityCondition) e.Tag).Name;
					break;
			}

			currentProgress.ProgressStep = (int) newProgressStep;

			string message = e.Tag as string;

			CallbackUtils.DoWithNonNull(message, s => currentProgress.Message = s);
			return true;
		}

		private static void UpdateNonContainerProgress(VerificationProgressMsg currentProgress,
		                                               VerificationProgressEventArgs e)
		{
			VerificationProgressStep newProgressStep = ToVerificationStep(e.ProgressStep);

			if (currentProgress.ProgressType != (int) e.ProgressType)
			{
				// First non-container progress
				ResetOverallStep(currentProgress);
				currentProgress.Message = string.Empty;
				currentProgress.ProcessingStepMessage = string.Empty;
			}

			if (newProgressStep == VerificationProgressStep.DataLoading)
			{
				SetOverallStep(currentProgress, e);
				ResetDetailStep(currentProgress);
				currentProgress.Message = $"Loading {((IReadOnlyDataset) e.Tag).Name}";
			}
			else if (newProgressStep == VerificationProgressStep.Testing)
			{
				SetDetailStep(currentProgress, e);
				currentProgress.Message = ((QualityCondition) e.Tag).Name;
			}

			currentProgress.ProgressStep = (int) newProgressStep;
		}

		private static void SetOverallStep([NotNull] VerificationProgressMsg progressMsg,
		                                   [NotNull] VerificationProgressEventArgs e)
		{
			if (e.Total == 0)
			{
				// no update
				return;
			}

			progressMsg.OverallProgressCurrentStep = e.Current + 1;
			progressMsg.OverallProgressTotalSteps = e.Total;
		}

		private static void SetDetailStep([NotNull] VerificationProgressMsg progressMsg,
		                                  [NotNull] VerificationProgressEventArgs e)
		{
			progressMsg.DetailedProgressCurrentStep = e.Current + 1;
			progressMsg.DetailedProgressTotalSteps = e.Total;
		}

		private static void ResetDetailStep([NotNull] VerificationProgressMsg progressMsg)
		{
			progressMsg.DetailedProgressCurrentStep = 0;
			progressMsg.DetailedProgressTotalSteps = 10;
		}

		private static void ResetOverallStep([NotNull] VerificationProgressMsg progressMsg)
		{
			progressMsg.OverallProgressCurrentStep = 0;
			progressMsg.OverallProgressTotalSteps = 10;
		}

		private static VerificationProgressStep ToVerificationStep(Step step)
		{
			switch (step)
			{
				case Step.DataLoading:
					return VerificationProgressStep.DataLoading;
				case Step.TileProcessing:
					return VerificationProgressStep.TileProcessing;
				case Step.ITestProcessing:
				case Step.TestRowCreated:
					return VerificationProgressStep.Testing;
				case Step.TileCompleting:
					return VerificationProgressStep.TileCompleting;
				default:
					return VerificationProgressStep.Undefined;
			}
		}

		public void AddPendingIssue([NotNull] IssueFoundEventArgs issueFoundEventArgs)
		{
			IssueMsg issueProto;
			if (BackgroundVerificationInputs != null)
			{
				issueProto = ProtobufQaUtils.CreateIssueProto(
					issueFoundEventArgs,
					Assert.NotNull(BackgroundVerificationInputs.VerificationContext));
			}
			else
			{
				issueProto = ProtobufQaUtils.CreateIssueProto(
					issueFoundEventArgs, KnownIssueSpatialReference, _standaloneIssueGeometryTypes);
			}

			_pendingIssues.Add(issueProto);
		}

		#endregion

		#region Final response

		private static ServiceCallStatus GetFinalCallStatus(
			[CanBeNull] QualityVerification verification,
			[CanBeNull] string qaServiceCancellationMessage)
		{
			ServiceCallStatus finalStatus;

			if (verification == null)
			{
				return ServiceCallStatus.Failed;
			}

			if (verification.Cancelled)
			{
				if (string.IsNullOrEmpty(qaServiceCancellationMessage))
				{
					finalStatus = ServiceCallStatus.Cancelled;
				}
				else
				{
					finalStatus = ServiceCallStatus.Failed;
				}
			}
			else
			{
				finalStatus = ServiceCallStatus.Finished;
			}

			return finalStatus;
		}

		private static bool TryGetVerificationResponse(
			T response,
			out VerificationResponse verificationResponse)
		{
			if (response is VerificationResponse result)
			{
				verificationResponse = result;
				return true;
			}

			if (response is DataVerificationResponse dataVerificationResponse)
			{
				verificationResponse = dataVerificationResponse.Response;
				return true;
			}

			verificationResponse = null;
			return false;
		}

		private static QualityVerificationMsg PackVerification(
			[CanBeNull] QualityVerification verification)
		{
			if (verification == null)
			{
				return null;
			}

			QualityVerificationMsg result = new QualityVerificationMsg();

			result.SavedVerificationId = verification.Id;
			result.SpecificationId = verification.SpecificationId;

			CallbackUtils.DoWithNonNull(
				verification.SpecificationName, s => result.SpecificationName = s);

			CallbackUtils.DoWithNonNull(
				verification.SpecificationDescription,
				s => result.SpecificationDescription = s);

			CallbackUtils.DoWithNonNull(verification.Operator, s => result.UserName = s);

			result.StartTimeTicks = verification.StartDate.Ticks;
			result.EndTimeTicks = verification.EndDate.Ticks;

			result.Fulfilled = verification.Fulfilled;
			result.Cancelled = verification.Cancelled;

			result.ProcessorTimeSeconds = verification.ProcessorTimeSeconds;

			CallbackUtils.DoWithNonNull(verification.ContextType, (s) => result.ContextType = s);
			CallbackUtils.DoWithNonNull(verification.ContextName, (s) => result.ContextName = s);

			result.RowsWithStopConditions = verification.RowsWithStopConditions;

			foreach (var conditionVerification in verification.ConditionVerifications)
			{
				var conditionVerificationMsg =
					new QualityConditionVerificationMsg
					{
						QualityConditionId =
							Assert.NotNull(conditionVerification.QualityCondition).Id,
						StopConditionId = conditionVerification.StopCondition?.Id ?? -1,
						Fulfilled = conditionVerification.Fulfilled,
						ErrorCount = conditionVerification.ErrorCount,
						ExecuteTime = conditionVerification.ExecuteTime,
						RowExecuteTime = conditionVerification.RowExecuteTime,
						TileExecuteTime = conditionVerification.TileExecuteTime
					};

				result.ConditionVerifications.Add(conditionVerificationMsg);
			}

			foreach (var verificationDataset in verification.VerificationDatasets)
			{
				var verificationDatasetMsg =
					new QualityVerificationDatasetMsg
					{
						DatasetId = verificationDataset.Dataset.Id,
						LoadTime = verificationDataset.LoadTime
					};

				result.VerificationDatasets.Add(verificationDatasetMsg);
			}

			return result;
		}

		#endregion
	}
}
