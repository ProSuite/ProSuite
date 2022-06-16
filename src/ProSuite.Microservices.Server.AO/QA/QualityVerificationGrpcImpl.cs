using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using Grpc.Core;
using log4net.Core;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using Quaestor.LoadReporting;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class QualityVerificationGrpcImpl : QualityVerificationGrpc.QualityVerificationGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staThreadScheduler;

		private static readonly ThreadAffineUseNameProvider _userNameProvider =
			new ThreadAffineUseNameProvider();

		private readonly Func<VerificationRequest, IBackgroundVerificationInputs>
			_verificationInputsFactoryMethod;

		private static DateTime _lastProgressTime = DateTime.MinValue;

		public QualityVerificationGrpcImpl(
			Func<VerificationRequest, IBackgroundVerificationInputs> inputsFactoryMethod,
			int maxThreadCount)
		{
			_verificationInputsFactoryMethod = inputsFactoryMethod;

			if (maxThreadCount <= 0)
			{
				maxThreadCount = Environment.ProcessorCount - 1;
			}

			_msg.DebugFormat("{0} parallel requests will be processed", maxThreadCount);

			_staThreadScheduler = new StaTaskScheduler(maxThreadCount);

			EnvironmentUtils.SetUserNameProvider(_userNameProvider);
		}

		/// <summary>
		/// The overall service process health. If it has been set, it will be marked as not serving
		/// in case any error occurs in this service implementation. Later this might be limited to
		/// specific, serious errors (such as out-of-memory, TNS could not be resolved).
		/// </summary>
		[CanBeNull]
		public IServiceHealth Health { get; set; }

		/// <summary>
		/// The current service load to be kept up-to-date by the quality verification service.
		/// A reference will also be passed to <see cref="LoadReportingGrpcImpl"/> to report the
		/// current load to interested load balancers. 
		/// </summary>
		[CanBeNull]
		public ServiceLoad CurrentLoad { get; set; }

		/// <summary>
		/// The license checkout action to be performed before any service call is executed.
		/// By default the lowest available license (basic, standard, advanced) is checked out
		/// in a 32-bit process, the server license is checked out in a 64-bit process. In case
		/// a test requires a specific license or an extension, provide a different function.
		/// </summary>
		[CanBeNull]
		public Func<bool> LicenseAction { get; set; }

		/// <summary>
		/// The report template path for HTML reports.
		/// </summary>
		[CanBeNull]
		public string HtmlReportTemplatePath { get; set; }

		/// <summary>
		/// The specification template path for the output HTML specification.
		/// </summary>
		[CanBeNull]
		public string QualitySpecificationTemplatePath { get; set; }

		public IList<XmlTestDescriptor> SupportedTestDescriptors { get; set; }

		public override async Task VerifyQuality(
			VerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ServerCallContext context)
		{
			try
			{
				await StartRequest(request);

				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel => VerifyQualityCore(request, responseStream, trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _staThreadScheduler);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
			}
			finally
			{
				EndRequest();
			}
		}

		public override async Task VerifyDataQuality(
			IAsyncStreamReader<DataVerificationRequest> requestStream,
			IServerStreamWriter<DataVerificationResponse> responseStream,
			ServerCallContext context)
		{
			VerificationRequest request = null;

			try
			{
				Assert.True(await requestStream.MoveNext(), "No request");

				DataVerificationRequest initialRequest =
					Assert.NotNull(requestStream.Current, "No request");

				request = initialRequest.Request;

				await StartRequest(request);

				Func<DataVerificationResponse, DataVerificationRequest> moreDataRequest =
					delegate(DataVerificationResponse r)
					{
						return Task.Run(async () =>
							                await RequestMoreDataAsync(
									                requestStream, responseStream, context, r)
								                .ConfigureAwait(false))
						           .Result;
					};

				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel =>
						VerifyDataQualityCore(initialRequest, moreDataRequest, responseStream,
						                      trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _staThreadScheduler);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
			}
			finally
			{
				EndRequest();
			}
		}

		public override async Task VerifyStandaloneXml(
			StandaloneVerificationRequest request,
			IServerStreamWriter<StandaloneVerificationResponse> responseStream,
			ServerCallContext context)
		{
			try
			{
				await StartRequest(context.Peer, request, true);

				_msg.InfoFormat("Starting stand-alone verification request from {0}",
				                context.Peer);
				_msg.VerboseDebug(() => $"Request details: {request}");

				Action<LoggingEvent> action =
					SendInfoLogAction(responseStream, ServiceCallStatus.Running);

				using (MessagingUtils.TemporaryRootAppender(new ActionAppender(action)))
				{
					Func<ITrackCancel, ServiceCallStatus> func =
						trackCancel => VerifyStandaloneXmlCore(request, responseStream,
						                                       trackCancel);

					ServiceCallStatus result =
						await GrpcServerUtils.ExecuteServiceCall(
							func, context, _staThreadScheduler);

					_msg.InfoFormat("Verification {0}", result);
				}
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
			}
			finally
			{
				EndRequest();
			}
		}

		private async Task StartRequest(VerificationRequest request)
		{
			await StartRequest(request.UserName, request, true);
		}

		private async Task StartRequest(string peerName, object request, bool requiresLicense)
		{
			CurrentLoad?.StartRequest();

			_msg.InfoFormat("Starting {0} request from {1}", request.GetType().Name, peerName);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(() => $"Request details: {request}");
			}

			if (requiresLicense)
			{
				bool licensed = await EnsureLicenseAsync();

				if (! licensed)
				{
					_msg.Warn("Could not check out the specified license");
				}
			}
		}

		private void EndRequest()
		{
			CurrentLoad?.EndRequest();
		}

		public async Task<bool> EnsureLicenseAsync()
		{
			if (LicenseAction == null)
			{
				return true;
			}

			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
			{
				return LicenseAction();
			}

			// Schedule it on an STA thread!
			CancellationToken cancellationToken = new CancellationToken(false);

			bool result =
				await Task.Factory.StartNew(
					LicenseAction, cancellationToken, TaskCreationOptions.LongRunning,
					_staThreadScheduler);

			return result;
		}

		private static Action<LoggingEvent> SendInfoLogAction(
			[NotNull] IServerStreamWriter<StandaloneVerificationResponse> responseStream,
			ServiceCallStatus callStatus)
		{
			Action<LoggingEvent> action =
				e =>
				{
					if (e.Level.Value < Level.Info.Value)
					{
						return;
					}

					var response = new StandaloneVerificationResponse
					               {
						               Message = new LogMsg
						                         {
							                         Message = e.RenderedMessage,
							                         MessageLevel = e.Level.Value
						                         },
						               ServiceCallStatus = (int) callStatus
					               };

					MessagingUtils.TrySendResponse(responseStream, response);
				};

			return action;
		}

		private static async Task<DataVerificationRequest> RequestMoreDataAsync(
			IAsyncStreamReader<DataVerificationRequest> requestStream,
			IServerStreamWriter<DataVerificationResponse> responseStream,
			ServerCallContext context,
			DataVerificationResponse r)
		{
			DataVerificationRequest resultData = null;

			Task responseReaderTask = Task.Run(
				async () =>
				{
					while (resultData == null)
					{
						while (await requestStream.MoveNext().ConfigureAwait(false))
						{
							resultData = requestStream.Current;
							break;
						}
					}
				});

			await responseStream.WriteAsync(r).ConfigureAwait(false);
			await responseReaderTask.ConfigureAwait(false);

			return resultData;
		}

		private ServiceCallStatus VerifyDataQualityCore(
			[NotNull] DataVerificationRequest initialRequest,
			Func<DataVerificationResponse, DataVerificationRequest> moreDataRequest,
			IServerStreamWriter<DataVerificationResponse> responseStream,
			ITrackCancel trackCancel)
		{
			var request = initialRequest.Request;

			SetupUserNameProvider(request);

			void SendResponse(VerificationResponse r) => responseStream.WriteAsync(
				new DataVerificationResponse {Response = r});

			BackgroundVerificationService qaService = null;
			List<GdbObjRefMsg> deletableAllowedErrorRefs = new List<GdbObjRefMsg>();
			QualityVerification verification = null;
			var issueCollection = new ConcurrentBag<IssueMsg>();
			string cancellationMessage = null;
			try
			{
				// TODO: Separate long-lived objects, such as datasetLookup, domainTransactions (add to this class) from
				// short-term objects (request) -> add to background verification inputs
				IBackgroundVerificationInputs backgroundVerificationInputs =
					_verificationInputsFactoryMethod(request);

				if (initialRequest.Schema != null)
				{
					backgroundVerificationInputs.SetGdbSchema(
						ProtobufConversionUtils.CreateSchema(initialRequest.Schema.ClassDefinitions,
						                                     initialRequest
							                                     .Schema.RelclassDefinitions,
						                                     moreDataRequest));
				}
				else if (moreDataRequest != null)
				{
					backgroundVerificationInputs.SetRemoteDataAccess(moreDataRequest);
				}

				qaService = CreateVerificationService(backgroundVerificationInputs, issueCollection,
				                                      SendResponse, trackCancel);

				verification = qaService.Verify(backgroundVerificationInputs, trackCancel);

				deletableAllowedErrorRefs.AddRange(
					GetDeletableAllowedErrorRefs(request.Parameters, qaService));
			}
			catch (Exception e)
			{
				_msg.Error($"Error checking quality for request {request}", e);
				cancellationMessage = $"Server error: {e.Message}";

				SetUnhealthy();
			}

			ServiceCallStatus result = SendFinalResponse(
				verification, cancellationMessage ?? qaService.CancellationMessage, issueCollection,
				deletableAllowedErrorRefs, qaService?.VerifiedPerimeter, SendResponse);

			return result;
		}

		private ServiceCallStatus VerifyQualityCore(
			VerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ITrackCancel trackCancel)
		{
			SetupUserNameProvider(request);

			void SendResponse(VerificationResponse r) => responseStream.WriteAsync(r);

			BackgroundVerificationService qaService = null;
			List<GdbObjRefMsg> deletableAllowedErrorRefs = new List<GdbObjRefMsg>();
			QualityVerification verification = null;
			var issueCollection = new ConcurrentBag<IssueMsg>();
			string cancellationMessage = null;
			try
			{
				// TODO: Separate long-lived objects, such as datasetLookup, domainTransactions (add to this class) from
				// short-term objects (request) -> add to background verification inputs
				IBackgroundVerificationInputs backgroundVerificationInputs =
					_verificationInputsFactoryMethod(request);

				qaService = CreateVerificationService(backgroundVerificationInputs, issueCollection,
				                                      SendResponse, trackCancel);

				int maxParallelRequested = request.MaxParallelProcessing;

				if (backgroundVerificationInputs.WorkerClient != null &&
				    maxParallelRequested > 1)
				{
					// allow directly adding issues found by client processes:
					qaService.DistributedTestRunner = new DistributedTestRunner(
						backgroundVerificationInputs.WorkerClient, request, issueCollection);
				}

				verification = qaService.Verify(backgroundVerificationInputs, trackCancel);

				deletableAllowedErrorRefs.AddRange(
					GetDeletableAllowedErrorRefs(request.Parameters, qaService));
			}
			catch (Exception e)
			{
				_msg.Error($"Error checking quality for request {request}", e);
				cancellationMessage = $"Server error: {e.Message}";

				if (! EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					    "PROSUITE_QA_SERVER_KEEP_SERVING_ON_ERROR"))
				{
					SetUnhealthy();
				}
			}

			ServiceCallStatus result = SendFinalResponse(
				verification, cancellationMessage ?? qaService.CancellationMessage, issueCollection,
				deletableAllowedErrorRefs, qaService?.VerifiedPerimeter, SendResponse);

			return result;
		}

		private ServiceCallStatus VerifyStandaloneXmlCore(
			StandaloneVerificationRequest request,
			IServerStreamWriter<StandaloneVerificationResponse> responseStream,
			ITrackCancel trackCancel)
		{
			// Machine login
			SetupUserNameProvider(Environment.UserName);

			try
			{
				VerificationParametersMsg parameters = request.Parameters;

				IGeometry perimeter =
					ProtobufGeometryUtils.FromShapeMsg(parameters.Perimeter);

				var aoi = perimeter == null ? null : new AreaOfInterest(perimeter);

				_msg.DebugFormat("Provided perimeter: {0}", GeometryUtils.ToString(perimeter));

				XmlBasedVerificationService qaService = new XmlBasedVerificationService(
					HtmlReportTemplatePath, QualitySpecificationTemplatePath);

				QualitySpecification qualitySpecification;

				switch (request.SpecificationCase)
				{
					case StandaloneVerificationRequest.SpecificationOneofCase.XmlSpecification:
					{
						XmlQualitySpecificationMsg xmlSpecification = request.XmlSpecification;

						qualitySpecification =
							SetupQualitySpecification(xmlSpecification, qaService);
						break;
					}
					case StandaloneVerificationRequest.SpecificationOneofCase
					                                  .ConditionListSpecification:
					{
						ConditionListSpecificationMsg conditionListSpec =
							request.ConditionListSpecification;

						qualitySpecification =
							SetupQualitySpecification(conditionListSpec, qaService);
						break;
					}
					default: throw new ArgumentOutOfRangeException();
				}

				qaService.ExecuteVerification(
					qualitySpecification, aoi, null, parameters.TileSize,
					request.OutputDirectory, IssueRepositoryType.FileGdb, trackCancel);
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error during processing of request {0}", request);
				_msg.Error($"Error verifying quality: {e.Message}", e);

				if (! EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					    "PROSUITE_QA_SERVER_KEEP_SERVING_ON_ERROR"))
				{
					SetUnhealthy();
				}

				return ServiceCallStatus.Failed;
			}

			// TODO: Final result message (error, warning count, row count with stop conditions, fulfilled)

			return trackCancel.Continue()
				       ? ServiceCallStatus.Finished
				       : ServiceCallStatus.Cancelled;
		}

		private static QualitySpecification SetupQualitySpecification(
			XmlQualitySpecificationMsg xmlSpecification, XmlBasedVerificationService qaService)
		{
			QualitySpecification qualitySpecification;
			var dataSources = new List<DataSource>();
			foreach (string replacement in xmlSpecification.DataSourceReplacements)
			{
				List<string> replacementStrings =
					StringUtils.SplitAndTrim(replacement, '|');
				Assert.AreEqual(2, replacementStrings.Count,
				                "Data source workspace is not of the format \"workspace_id | catalog_path\"");

				var dataSource =
					new DataSource(replacementStrings[0], replacementStrings[0])
					{
						WorkspaceAsText = replacementStrings[1]
					};

				dataSources.Add(dataSource);
			}

			qualitySpecification =
				qaService.SetupQualitySpecification(
					xmlSpecification.Xml, xmlSpecification.SelectedSpecificationName,
					dataSources);
			return qualitySpecification;
		}

		private QualitySpecification SetupQualitySpecification(
			ConditionListSpecificationMsg conditionsSpecificationMsg,
			XmlBasedVerificationService qaService)
		{
			if (SupportedTestDescriptors == null || SupportedTestDescriptors.Count == 0)
			{
				throw new InvalidOperationException(
					"No xml test descriptors have been set up.");
			}

			var dataSources = conditionsSpecificationMsg.DataSources.Select(
				dsMsg => new DataSource(dsMsg.ModelName, dsMsg.Id, dsMsg.CatalogPath,
				                        dsMsg.Database, dsMsg.SchemaOwner)).ToList();

			_msg.DebugFormat("{0} data sources provided:{1} {2}",
			                 dataSources.Count, Environment.NewLine,
			                 StringUtils.Concatenate(dataSources, Environment.NewLine));

			var specificationElements = new List<SpecificationElement>();

			foreach (QualitySpecificationElementMsg specificationElementMsg in
			         conditionsSpecificationMsg.Elements)
			{
				// Temporary - TODO: Remove de-tour via xml condition

				SpecificationElement specificationElement =
					ProtobufConversionUtils.CreateXmlConditionElement(specificationElementMsg);

				specificationElements.Add(specificationElement);

				//using (TextReader xmlReader = new StringReader(conditionXml))
				//{
				//	XmlQualityCondition condition =
				//		XmlDataQualityUtils.DeserializeCondition(xmlReader);

				//	var specificationElement =
				//		new SpecificationElement(condition,
				//		                         specificationElementMsg.CategoryName)
				//		{
				//			AllowErrors = specificationElementMsg.AllowErrors,
				//			StopOnError = specificationElementMsg.StopOnError
				//		};

				//	specificationElements.Add(specificationElement);
				//}
			}

			QualitySpecification qualitySpecification = qaService.SetupQualitySpecification(
				conditionsSpecificationMsg.Name, SupportedTestDescriptors, specificationElements,
				dataSources, false);

			return qualitySpecification;
		}

		private static IEnumerable<GdbObjRefMsg> GetDeletableAllowedErrorRefs(
			VerificationParametersMsg requestParameters,
			BackgroundVerificationService qaService)
		{
			// Add invalidated allowed errors to be deleted
			if (requestParameters.ReportInvalidExceptions)
			{
				foreach (AllowedError allowedError in qaService.GetInvalidatedAllowedErrors())
				{
					yield return GetGdbObjRefMsg(allowedError);
				}
			}

			if (requestParameters.ReportUnusedExceptions)
			{
				foreach (AllowedError allowedError in qaService.GetUnusedAllowedErrors())
				{
					yield return GetGdbObjRefMsg(allowedError);
				}
			}
		}

		private static BackgroundVerificationService CreateVerificationService(
			IBackgroundVerificationInputs backgroundVerificationInputs,
			ConcurrentBag<IssueMsg> issueCollection,
			Action<VerificationResponse> writeAction, ITrackCancel trackCancel)
		{
			var qaService = new BackgroundVerificationService(
				                backgroundVerificationInputs.DomainTransactions,
				                backgroundVerificationInputs.DatasetLookup)
			                {
				                CustomErrorFilter = backgroundVerificationInputs.CustomErrorFilter
			                };

			var currentProgress = new VerificationProgressMsg();

			qaService.IssueFound +=
				(sender, args) =>
					issueCollection.Add(
						IssueProtobufUtils.CreateIssueProto(args, backgroundVerificationInputs));

			qaService.Progress += (sender, args) =>
				SendProgress(
					sender, args, issueCollection,
					currentProgress, writeAction, trackCancel);

			return qaService;
		}

		private static void SetupUserNameProvider(VerificationRequest request)
		{
			string userName = request.UserName;

			SetupUserNameProvider(userName);
		}

		private static void SetupUserNameProvider(string userName)
		{
			_msg.DebugFormat("New verification request from {0}", userName);

			if (! string.IsNullOrEmpty(userName))
			{
				_userNameProvider.SetDisplayName(userName);
			}
		}

		private static void SendProgress(
			object sender,
			VerificationProgressEventArgs args,
			ConcurrentBag<IssueMsg> issueCollection,
			VerificationProgressMsg currentProgress,
			Action<VerificationResponse> writeAction,
			ITrackCancel trackCancel)
		{
			if (trackCancel != null && ! trackCancel.Continue())
			{
				_msg.Debug("Cancelling...");
				((QualityVerificationServiceBase) sender).Cancel();

				return;
			}

			SendProgress(args, issueCollection, currentProgress, writeAction);
		}

		private static void SendProgress(VerificationProgressEventArgs args,
		                                 ConcurrentBag<IssueMsg> issueCollection,
		                                 VerificationProgressMsg currentProgress,
		                                 Action<VerificationResponse> writeAction)
		{
			if (! IsPriorityProgress(args, currentProgress, issueCollection) &&
			    DateTime.Now - _lastProgressTime < TimeSpan.FromSeconds(1))
			{
				return;
			}

			_lastProgressTime = DateTime.Now;

			WriteProgressAndIssues(args, issueCollection, currentProgress, writeAction);
		}

		private static bool IsPriorityProgress(VerificationProgressEventArgs args,
		                                       VerificationProgressMsg currentProgress,
		                                       ConcurrentBag<IssueMsg> issueCollection)
		{
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

			return currentProgress.ProgressStep != (int) args.ProgressStep;
		}

		private static bool IsRelevantStep(Step progressStep)
		{
			return ToVerificationStep(progressStep) != VerificationProgressStep.Undefined;
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

		private static GdbObjRefMsg GetGdbObjRefMsg(AllowedError allowedError)
		{
			GdbObjectReference objReference = allowedError.GetObjectReference();

			var result =
				new GdbObjRefMsg()
				{
					ClassHandle = objReference.ClassId,
					ObjectId = objReference.ObjectId
				};

			return result;
		}

		private static void WriteProgressAndIssues(
			VerificationProgressEventArgs e,
			ConcurrentBag<IssueMsg> issues,
			VerificationProgressMsg currentProgress,
			Action<VerificationResponse> writeAction)
		{
			VerificationResponse response = new VerificationResponse
			                                {
				                                ServiceCallStatus = (int) ServiceCallStatus.Running
			                                };

			if (! UpdateProgress(currentProgress, e) && issues.Count == 0)
			{
				return;
			}

			response.Progress = currentProgress;

			//List<IssueMsg> sentIssues = new List<IssueMsg>(issues.Count);

			while (issues.TryTake(out IssueMsg issue))
			{
				response.Issues.Add(issue);
			}

			_msg.DebugFormat("Sending {0} errors back to client...", issues.Count);

			try
			{
				writeAction(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.VerboseDebug(() => "Error sending progress to the client", ex);

				// The issues would be lost, so put them back into the collection
				foreach (IssueMsg issue in response.Issues)
				{
					issues.Add(issue);
				}
			}
		}

		private static ServiceCallStatus SendFinalResponse(
			[CanBeNull] QualityVerification verification,
			[CanBeNull] string qaServiceCancellationMessage,
			ConcurrentBag<IssueMsg> issues,
			List<GdbObjRefMsg> deletableAllowedErrors,
			[CanBeNull] IEnvelope verifiedPerimeter,
			Action<VerificationResponse> writeAction)
		{
			var response = new VerificationResponse();

			while (issues.TryTake(out IssueMsg issue))
			{
				response.Issues.Add(issue);
			}

			response.ObsoleteExceptions.AddRange(deletableAllowedErrors);

			ServiceCallStatus finalStatus =
				GetFinalCallStatus(verification, qaServiceCancellationMessage);

			response.ServiceCallStatus = (int) finalStatus;

			response.Progress = new VerificationProgressMsg();

			if (! string.IsNullOrEmpty(qaServiceCancellationMessage))
			{
				response.Progress.Message = qaServiceCancellationMessage;
			}

			// Ensure that progress is at 100%:
			response.Progress.OverallProgressCurrentStep = 10;
			response.Progress.OverallProgressTotalSteps = 10;
			response.Progress.DetailedProgressCurrentStep = 10;
			response.Progress.DetailedProgressTotalSteps = 10;

			PackVerification(verification, response);

			if (verifiedPerimeter != null)
			{
				response.VerifiedPerimeter =
					ProtobufGeometryUtils.ToShapeMsg(verifiedPerimeter);
			}

			_msg.DebugFormat(
				"Sending final message with {0} errors back to client...",
				issues.Count);

			try
			{
				writeAction(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.Warn(
					"Error sending progress to the client. Retrying the last response in 1s...",
					ex);

				// Re-try (only for final message)
				Task.Delay(1000);
				writeAction(response);
			}

			return finalStatus;
		}

		private static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<VerificationResponse> responseStream)
		{
			void Write(VerificationResponse r) => responseStream.WriteAsync(r);

			SendFatalException(exception, Write);
		}

		private static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<DataVerificationResponse> responseStream)
		{
			void Write(VerificationResponse r) =>
				responseStream.WriteAsync(new DataVerificationResponse {Response = r});

			SendFatalException(exception, Write);
		}

		private static void SendFatalException(
			[NotNull] Exception exception,
			Action<VerificationResponse> writeAsync)
		{
			var response = new VerificationResponse();

			response.ServiceCallStatus = (int) ServiceCallStatus.Failed;

			if (! string.IsNullOrEmpty(exception.Message))
			{
				response.Progress = new VerificationProgressMsg
				                    {
					                    Message = exception.Message
				                    };
			}

			try
			{
				writeAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.Warn("Error sending progress to the client", ex);
			}
		}

		private static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<StandaloneVerificationResponse> responseStream)
		{
			MessagingUtils.SendResponse(responseStream,
			                            new StandaloneVerificationResponse()
			                            {
				                            Message = new LogMsg()
				                                      {
					                                      Message = exception.Message,
					                                      MessageLevel = Level.Error.Value
				                                      },
				                            ServiceCallStatus = (int) ServiceCallStatus.Failed
			                            });
		}

		private void SetUnhealthy()
		{
			if (Health != null)
			{
				_msg.Warn("Setting service health to \"not serving\" due to exception " +
				          "because the process might be compromised.");

				Health?.SetStatus(GetType(), false);
			}
		}

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

		private static void PackVerification([CanBeNull] QualityVerification verification,
		                                     [NotNull] VerificationResponse response)
		{
			if (verification == null)
			{
				return;
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

			response.QualityVerification = result;
		}

		#region Progress

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

		private static void SetMessageFromCondition(VerificationProgressMsg progressMsg,
		                                            VerificationProgressEventArgs e)
		{
			progressMsg.Message =
				e.ProgressType == VerificationProgressType.ProcessNonCache
					? ((QualityCondition) e.Tag).Name
					: string.Empty;
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

		#endregion
	}
}
