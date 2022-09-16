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
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
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

				var responseStreamer =
					new VerificationProgressStreamer<StandaloneVerificationResponse>(
						responseStream);

				responseStreamer.CreateResponseAction = responseStreamer.CreateStandaloneResponse;

				Action<LoggingEvent> action =
					SendStandaloneProgressAction(responseStreamer, ServiceCallStatus.Running);

				ServiceCallStatus result;
				using (MessagingUtils.TemporaryRootAppender(new ActionAppender(action)))
				{
					Func<ITrackCancel, ServiceCallStatus> func =
						trackCancel =>
							VerifyStandaloneXmlCore(request, responseStreamer, trackCancel);

					result = await GrpcServerUtils.ExecuteServiceCall(
						         func, context, _staThreadScheduler);

					// final message:
					responseStreamer.WriteProgressAndIssues(
						new VerificationProgressEventArgs($"Verification {result}"), result);
				}

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

		private static Action<LoggingEvent> SendStandaloneProgressAction(
			[NotNull] VerificationProgressStreamer<StandaloneVerificationResponse> progressStreamer,
			ServiceCallStatus callStatus)
		{
			Action<LoggingEvent> action =
				e =>
				{
					if (e.Level.Value < Level.Info.Value)
					{
						return;
					}

					progressStreamer.CurrentLogLevel = e.Level.Value;

					VerificationProgressEventArgs progressEventArgs =
						new VerificationProgressEventArgs(e.RenderedMessage);

					progressStreamer.WriteProgressAndIssues(progressEventArgs, callStatus);
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

			BackgroundVerificationService qaService = null;
			VerificationProgressStreamer<DataVerificationResponse> responseStreamer =
				new VerificationProgressStreamer<DataVerificationResponse>(responseStream);

			List<GdbObjRefMsg> deletableAllowedErrorRefs = new List<GdbObjRefMsg>();
			QualityVerification verification = null;
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

				responseStreamer.BackgroundVerificationInputs = backgroundVerificationInputs;
				responseStreamer.CreateResponseAction =
					responseStreamer.CreateDataVerificationResponse;

				qaService = CreateVerificationService(
					backgroundVerificationInputs, responseStreamer, trackCancel);

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

			ServiceCallStatus result = responseStreamer.SendFinalResponse(verification,
				cancellationMessage ?? qaService.CancellationMessage, deletableAllowedErrorRefs,
				qaService?.VerifiedPerimeter);

			return result;
		}

		private ServiceCallStatus VerifyQualityCore(
			VerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ITrackCancel trackCancel)
		{
			SetupUserNameProvider(request);

			BackgroundVerificationService qaService = null;
			VerificationProgressStreamer<VerificationResponse> responseStreamer =
				new VerificationProgressStreamer<VerificationResponse>(responseStream);

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

				responseStreamer.BackgroundVerificationInputs = backgroundVerificationInputs;
				responseStreamer.CreateResponseAction = responseStreamer.CreateVerificationResponse;

				qaService = CreateVerificationService(
					backgroundVerificationInputs, responseStreamer, trackCancel);

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

			ServiceCallStatus result = responseStreamer.SendFinalResponse(verification,
				cancellationMessage ?? qaService.CancellationMessage, deletableAllowedErrorRefs,
				qaService?.VerifiedPerimeter);

			return result;
		}

		private ServiceCallStatus VerifyStandaloneXmlCore(
			StandaloneVerificationRequest request,
			VerificationProgressStreamer<StandaloneVerificationResponse> responseStreamer,
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

				qaService.IssueFound +=
					(sender, args) => responseStreamer.AddPendingIssue(args);

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

				Model primaryModel =
					StandaloneVerificationUtils.GetPrimaryModel(qualitySpecification);
				responseStreamer.KnownIssueSpatialReference =
					primaryModel?.SpatialReferenceDescriptor.SpatialReference;

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

		private static BackgroundVerificationService CreateVerificationService<T>(
			IBackgroundVerificationInputs backgroundVerificationInputs,
			VerificationProgressStreamer<T> responseStreamer, ITrackCancel trackCancel)
			where T : class
		{
			var qaService = new BackgroundVerificationService(
				                backgroundVerificationInputs.DomainTransactions,
				                backgroundVerificationInputs.DatasetLookup)
			                {
				                CustomErrorFilter = backgroundVerificationInputs.CustomErrorFilter
			                };

			qaService.IssueFound +=
				(sender, args) => responseStreamer.AddPendingIssue(args);

			qaService.Progress += (sender, args) =>
				SendProgress(
					sender, args, responseStreamer, trackCancel);

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

		private static void SendProgress<T>(
			object sender,
			VerificationProgressEventArgs args,
			VerificationProgressStreamer<T> responseStreamer,
			ITrackCancel trackCancel) where T : class
		{
			if (trackCancel != null && ! trackCancel.Continue())
			{
				_msg.Debug("Cancelling...");
				((QualityVerificationServiceBase) sender).Cancel();

				return;
			}

			responseStreamer.WriteProgressAndIssues(args, ServiceCallStatus.Running);
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
	}
}
