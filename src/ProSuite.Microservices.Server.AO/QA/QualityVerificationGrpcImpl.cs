using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Grpc.Core;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class QualityVerificationGrpcImpl : QualityVerificationGrpc.QualityVerificationGrpcBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly StaTaskScheduler _singleStaThreadScheduler;

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

			_singleStaThreadScheduler = new StaTaskScheduler(maxThreadCount);

			EnvironmentUtils.SetUserNameProvider(_userNameProvider);
		}

		/// <summary>
		/// The overall service process health. If it has been set, it will be marked as not serving
		/// in case any error occurs in this service implementation. Later this might be limited to
		/// specific, serious errors (such as out-of-memory, TNS could not be resolved).
		/// </summary>
		public IServiceHealth Health { get; set; }

		public bool Checkout3DAnalyst { get; set; }

		public override async Task VerifyQuality(
			VerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ServerCallContext context)
		{
			try
			{
				_msg.InfoFormat("Starting verification request from {0}", request.UserName);
				_msg.DebugFormat("Request details: {0}", request);

				if (Checkout3DAnalyst)
				{
					// It must be re-checked out (but somehow it's enough to do it
					// on the calling thread-pool thread!?)
					Ensure3dAnalyst();
				}

				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel => VerifyQualityCore(request, responseStream, trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _singleStaThreadScheduler);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
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

				//while (await requestStream.MoveNext())
				DataVerificationRequest initialrequest =
					Assert.NotNull(requestStream.Current, "No request");

				request = initialrequest.Request;

				_msg.InfoFormat("Starting verification request from {0}", request);
				_msg.DebugFormat("Request details: {0}", request);

				if (Checkout3DAnalyst)
				{
					// It must be re-checked out (but somehow it's enough to do it
					// on the calling thread-pool thread!?)
					Ensure3dAnalyst();
				}

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
						VerifyDataQualityCore(initialrequest, moreDataRequest, responseStream,
						                      trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _singleStaThreadScheduler);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
			}
		}

		public override async Task VerifyStandaloneXml(
			StandaloneVerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ServerCallContext context)
		{
			try
			{
				_msg.InfoFormat("Starting standa alone verification request from {0}",
				                context.Peer);
				_msg.DebugFormat("Request details: {0}", request);

				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel => VerifyStandaloneXmlCore(request, responseStream, trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _singleStaThreadScheduler);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SendFatalException(e, responseStream);
				SetUnhealthy();
			}
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

		private static void Ensure3dAnalyst()
		{
			IAoInitialize aoInitialize = new AoInitializeClass();

			bool is3dAvailable =
				aoInitialize.IsExtensionCheckedOut(
					esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst);

			if (! is3dAvailable)
			{
				esriLicenseStatus status =
					aoInitialize.CheckOutExtension(
						esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst);

				_msg.DebugFormat("3D Analyst checkout status: {0}", status);
			}
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
			IServerStreamWriter<VerificationResponse> responseStream,
			ITrackCancel trackCancel)
		{
			// Machine login
			SetupUserNameProvider(Environment.UserName);

			// TODO: Re-direct log messages
			void SendResponse(VerificationResponse r) => responseStream.WriteAsync(r);

			try
			{
				VerificationParametersMsg parameters = request.Parameters;

				IGeometry perimeter =
					ProtobufGeometryUtils.FromShapeMsg(parameters.Perimeter);

				XmlBasedVerificationService qaService =
					CreateXmlBasedStandaloneService(request, SendResponse, trackCancel);

				XmlQualitySpecificationMsg xmlSpecification = request.Specification;

				var aoi = perimeter == null ? null : new AreaOfInterest(perimeter);
				qaService.ExecuteVerification(
					xmlSpecification.Xml,
					xmlSpecification.SelectedSpecificationName,
					xmlSpecification.DataSourceReplacements, aoi, null, parameters.TileSize,
					parameters.IssueFileGdbPath, IssueRepositoryType.FileGdb,
					true, trackCancel);
			}
			catch (Exception e)
			{
				_msg.Error($"Error checking quality for request {request}", e);

				if (! EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					    "PROSUITE_QA_SERVER_KEEP_SERVING_ON_ERROR"))
				{
					SetUnhealthy();
				}

				return ServiceCallStatus.Failed;
			}

			return trackCancel.Continue()
				       ? ServiceCallStatus.Finished
				       : ServiceCallStatus.Cancelled;
		}

		private XmlBasedVerificationService CreateXmlBasedStandaloneService(
			[NotNull] StandaloneVerificationRequest request,
			[NotNull] Action<VerificationResponse> writeAction,
			ITrackCancel trackCancel)
		{
			// From local xml options?
			string specificationTemplatePath = null;
			XmlBasedVerificationService xmlService = new XmlBasedVerificationService(
				request.Parameters.HtmlTemplatePath,
				specificationTemplatePath);

			return xmlService;
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
					issueCollection.Add(CreateIssueProto(args, backgroundVerificationInputs));

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

		private static IssueMsg CreateIssueProto(
			[NotNull] IssueFoundEventArgs args,
			[NotNull] IBackgroundVerificationInputs backgroundVerificationInputs)
		{
			QualityCondition qualityCondition =
				args.QualitySpecificationElement.QualityCondition;

			IssueMsg issueProto = new IssueMsg();

			issueProto.ConditionId = qualityCondition.Id;
			issueProto.Allowable = args.IsAllowable;
			issueProto.StopCondition = args.Issue.StopCondition;

			CallbackUtils.DoWithNonNull(
				args.Issue.Description, s => issueProto.Description = s);

			IssueCode issueCode = args.Issue.IssueCode;

			if (issueCode != null)
			{
				CallbackUtils.DoWithNonNull(
					issueCode.ID, s => issueProto.IssueCodeId = s);

				CallbackUtils.DoWithNonNull(
					issueCode.Description, s => issueProto.IssueCodeDescription = s);
			}

			CallbackUtils.DoWithNonNull(
				args.Issue.AffectedComponent,
				(value) => issueProto.AffectedComponent = value);

			issueProto.InvolvedTables.AddRange(GetInvolvedTableMessages(args.Issue.InvolvedTables));

			CallbackUtils.DoWithNonNull(
				args.LegacyInvolvedObjectsString,
				(value) => issueProto.LegacyInvolvedRows = value);

			IVerificationContext verificationContext =
				Assert.NotNull(backgroundVerificationInputs.VerificationContext);

			var supportedGeometryTypes =
				GetSupportedErrorRepoGeometryTypes(verificationContext).ToList();

			// create valid Error geometry (geometry type, min dimensions) if possible
			IGeometry geometry = ErrorRepositoryUtils.GetGeometryToStore(
				args.ErrorGeometry,
				verificationContext.SpatialReferenceDescriptor.SpatialReference,
				supportedGeometryTypes);

			issueProto.IssueGeometry =
				ProtobufGeometryUtils.ToShapeMsg(geometry);

			issueProto.CreationDateTimeTicks = DateTime.Now.Ticks;

			//issueProto.IsInvalidException = args.us;

			//if (args.IsAllowed)
			//{
			//	issueProto.ExceptedObjRef = new GdbObjRefMsg()
			//	                            {
			//		                            ClassHandle = args.AllowedErrorRef.ClassId,
			//		                            ObjectId = args.AllowedErrorRef.ObjectId
			//	                            };
			//}

			return issueProto;
		}

		private static IEnumerable<esriGeometryType> GetSupportedErrorRepoGeometryTypes(
			IVerificationContext verificationContext)
		{
			if (verificationContext.NoGeometryIssueDataset != null)
				yield return esriGeometryType.esriGeometryNull;

			if (verificationContext.MultipointIssueDataset != null)
				yield return esriGeometryType.esriGeometryMultipoint;

			if (verificationContext.LineIssueDataset != null)
				yield return esriGeometryType.esriGeometryPolyline;

			if (verificationContext.PolygonIssueDataset != null)
				yield return esriGeometryType.esriGeometryPolygon;

			if (verificationContext.MultiPatchIssueDataset != null)
				yield return esriGeometryType.esriGeometryMultiPatch;
		}

		private static IEnumerable<InvolvedTableMsg> GetInvolvedTableMessages(
			IEnumerable<InvolvedTable> involvedTables)
		{
			foreach (InvolvedTable involvedTable in involvedTables)
			{
				var involvedTableMsg = new InvolvedTableMsg();
				involvedTableMsg.TableName = involvedTable.TableName;

				yield return involvedTableMsg;
			}
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

			//response.Issues.AddRange(issues);

			_msg.DebugFormat("Sending {0} errors back to client...", issues.Count);

			try
			{
				writeAction(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.VerboseDebug("Error sending progress to the client", ex);

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

			if (! string.IsNullOrEmpty(qaServiceCancellationMessage))
			{
				response.Progress = new VerificationProgressMsg
				                    {
					                    Message = qaServiceCancellationMessage
				                    };
			}

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
					currentProgress.Message = ((IDataset) e.Tag).Name;
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
				currentProgress.Message = $"Loading {((IDataset) e.Tag).Name}";
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
