using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Grpc.Core;
using log4net.Core;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.AO.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.QA.Distributed;
using ProSuite.QA.Container;
using Quaestor.LoadReporting;
using Quaestor.ProcessAdministration;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class QualityVerificationGrpcImpl : QualityVerificationGrpc.QualityVerificationGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staThreadScheduler;

		private readonly Func<VerificationRequest, IBackgroundVerificationInputs>
			_verificationInputsFactoryMethod;

		private bool _licensed;

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

			var perThreadUserNameProvider =
				EnvironmentUtils.GetUserNameProvider() as ThreadAffineUseNameProvider;

			if (perThreadUserNameProvider == null)
			{
				var userNameProvider = new ThreadAffineUseNameProvider();
				EnvironmentUtils.SetUserNameProvider(userNameProvider);
			}
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
		/// Admin interface to manage requests and their cancellation.
		/// </summary>
		public IRequestAdmin RequestAdmin { get; set; }

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

		/// <summary>
		/// The supported test descriptors for a fine-granular specification based off a condition list.
		/// </summary>
		[CanBeNull]
		public ISupportedInstanceDescriptors SupportedInstanceDescriptors { get; set; }

		/// <summary>
		/// The client end point used for parallel processing.
		/// </summary>
		[CanBeNull]
		public DistributedWorkers DistributedProcessingClients { get; set; }

		/// <summary>
		/// The default value to use if the environment variable that indicates whether or not the
		/// service should continue serving (or shut down) in case of an exception.
		/// </summary>
		public bool KeepServingOnErrorDefaultValue { get; set; }

		/// <summary>
		/// Whether the service should be set to unhealthy after each verification. This allows
		/// for process recycling after each verification to avoid GDB-locks.
		/// </summary>
		public bool SetUnhealthyAfterEachVerification { get; set; } =
			EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_QA_SERVER_SET_UNHEALTHY_AFTER_VERIFICATION");

		/// <summary>
		/// Whether the service should be set to unhealthy after a verification when the memory
		/// allocation (private bytes) exceeds the specified amount in MB. This allows
		/// for process recycling after verifications that fragment the process memory
		/// which can lead to excessive memory pressure on the host.
		/// </summary>
		public double UnhealthyMemoryLimitMegaBytes { get; set; } = GetUnhealthyMemoryLimit();

		public override async Task VerifyQuality(
			VerificationRequest request,
			IServerStreamWriter<VerificationResponse> responseStream,
			ServerCallContext context)
		{
			CancelableRequest registeredRequest = null;
			try
			{
				await StartRequest(request);

				registeredRequest =
					RegisterRequest(request.UserName, request.Environment,
					                context.CancellationToken);

				var trackCancellationToken =
					new TrackCancellationToken(registeredRequest.CancellationSource.Token);

				// TODO: Adapt ITrackCancel, move to CancellationToken everywhere.
				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel =>
						VerifyQualityCore(request, responseStream, trackCancellationToken);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _staThreadScheduler, true);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (TaskCanceledException canceledException)
			{
				HandleCancellationException(request, context, canceledException);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				ServiceUtils.SendFatalException(e, responseStream);
				ServiceUtils.SetUnhealthy(Health, GetType());
			}
			finally
			{
				if (registeredRequest != null)
				{
					RequestAdmin.UnregisterRequest(registeredRequest);
				}

				EndRequest();
			}
		}

		/// <summary>
		/// Verifies the data quality with various levels of data being provided:
		/// - Standalone (request.WorkContext is null) proto-based specification:
		///   -> If the Schema is delivered by caller (i.e. the data model), harvesting can be omitted
		///      but the data sources need to have a working catalog path as connection to read the data.
		///      Data streaming is not (yet) supported.
		///	     This is useful in distributed scenarios to avoid re-harvesting in each sub-verification.
		/// - Ddx-based (WorkContext.Type > 0):
		///   -> If the Schema is delivered by caller (i.e. the actual virtual workspace(s)) it is used
		///      for data access (using the DataVerificationRequest in a response)
		///   -> If the Schema is not delivered by caller: The Ddx model is used and the schema/data is
		///      requested from the client.
		/// </summary>
		/// <param name="requestStream"></param>
		/// <param name="responseStream"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task VerifyDataQuality(
			IAsyncStreamReader<DataVerificationRequest> requestStream,
			IServerStreamWriter<DataVerificationResponse> responseStream,
			ServerCallContext context)
		{
			VerificationRequest request = null;
			CancelableRequest registeredRequest = null;

			try
			{
				Assert.True(await requestStream.MoveNext(), "No request");

				DataVerificationRequest initialRequest =
					Assert.NotNull(requestStream.Current, "No request");

				request = initialRequest.Request;

				await StartRequest(request);

				registeredRequest =
					RegisterRequest(request.UserName, request.Environment,
					                context.CancellationToken);

				var trackCancellationToken =
					new TrackCancellationToken(registeredRequest.CancellationSource.Token);

				// TODO: Separate data request handler class with async method
				Func<DataVerificationResponse, DataVerificationRequest> moreDataRequest =
					delegate(DataVerificationResponse r)
					{
						Task<DataVerificationRequest> task = RequestMoreDataAsync(
							requestStream, responseStream, context, r);

						long timeOutMillis = 30 * 1000;
						long elapsedMillis = 0;
						int interval = 20;
						while (! task.IsCompleted && elapsedMillis < timeOutMillis)
						{
							Thread.Sleep(interval);
							elapsedMillis += interval;
						}

						if (task.IsFaulted)
						{
							throw task.Exception;
						}

						if (! task.IsCompleted)
						{
							throw new TimeoutException(
								$"Client failed to provide data within {elapsedMillis}ms");
						}

						DataVerificationRequest moreData = task.Result;

						return moreData;
					};

				Func<ITrackCancel, ServiceCallStatus> func =
					trackCancel =>
						VerifyDataQualityCore(initialRequest, moreDataRequest, responseStream,
						                      trackCancellationToken);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _staThreadScheduler, true);

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (TaskCanceledException canceledException)
			{
				HandleCancellationException(request, context, canceledException);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				ServiceUtils.SendFatalException(e, responseStream);
				ServiceUtils.SetUnhealthy(Health, GetType());
			}
			finally
			{
				if (registeredRequest != null)
				{
					RequestAdmin.UnregisterRequest(registeredRequest);
				}

				EndRequest();
			}
		}

		public override async Task QueryData(IAsyncStreamReader<QueryDataRequest> requestStream,
		                                     IServerStreamWriter<QueryDataResponse> responseStream,
		                                     ServerCallContext context)
		{
			QueryDataRequest request = null;
			try
			{
				Stopwatch watch = Stopwatch.StartNew();

				Assert.True(await requestStream.MoveNext(), "No request");

				request = Assert.NotNull(requestStream.Current, "No request");

				await StartRequest(context.Peer, request, true);

				// TODO: Separate data request handler class with async method
				Func<QueryDataResponse, QueryDataRequest> moreDataRequest = null;

				bool useclientData =
					request.DataSources.Any(ds => string.IsNullOrEmpty(ds.CatalogPath));

				if (useclientData)
				{
					moreDataRequest =
						delegate(QueryDataResponse r)
						{
							Task<QueryDataRequest> task = RequestMoreDataAsync(
								requestStream, responseStream, context, r);

							long timeOutMillis = 30 * 1000;
							long elapsedMillis = 0;
							int interval = 20;
							while (! task.IsCompleted && elapsedMillis < timeOutMillis)
							{
								Thread.Sleep(interval);
								elapsedMillis += interval;
							}

							if (task.IsFaulted)
							{
								throw task.Exception;
							}

							if (! task.IsCompleted)
							{
								throw new TimeoutException(
									$"Client failed to provide data within {elapsedMillis}ms");
							}

							QueryDataRequest moreData = task.Result;

							return moreData;
						};
				}

				Func<ITrackCancel, ServiceCallStatus> func = trackCancel =>
					QueryDataCore(request, responseStream, moreDataRequest, trackCancel);

				ServiceCallStatus result =
					await GrpcServerUtils.ExecuteServiceCall(
						func, context, _staThreadScheduler, true);

				watch.Stop();

				_msg.InfoFormat("Data request {0} ({1} ms)", result, watch.ElapsedMilliseconds);
			}
			catch (TaskCanceledException canceledException)
			{
				HandleCancellationException(request, context, canceledException);
			}
			catch (Exception e)
			{
				_msg.Error($"Error querying data for request {request}", e);

				ServiceUtils.SendFatalException(e, responseStream);
				ServiceUtils.SetUnhealthy(Health, GetType());
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

				// Attach to logging infrastructure
				Action<LoggingEvent> action =
					SendStandaloneProgressAction(responseStreamer, ServiceCallStatus.Running);

				ServiceCallStatus result;
				using (MessagingUtils.TemporaryRootAppender(new ActionAppender(action)))
				{
					Func<ITrackCancel, ServiceCallStatus> func =
						trackCancel =>
							VerifyStandaloneXmlCore(request, responseStreamer, trackCancel);

					result = await GrpcServerUtils.ExecuteServiceCall(
						         func, context, _staThreadScheduler, true);

					// final message:
					responseStreamer.WriteProgressAndIssues(
						new VerificationProgressEventArgs($"Verification {result}"), result);
				}

				_msg.InfoFormat("Verification {0}", result);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				ServiceUtils.SendFatalException(e, responseStream);
				ServiceUtils.SetUnhealthy(Health, GetType());
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
			// The request comes in on a .NET thread-pool thread, which has no useful name
			// when it comes to logging. Set the ID as its name.
			ProcessUtils.TrySetThreadIdAsName();

			CurrentLoad?.StartRequest();

			string concurrentRequestMsg =
				CurrentLoad == null
					? string.Empty
					: $"Concurrently running requests (including this one): {CurrentLoad.CurrentProcessCount}";

			_msg.InfoFormat("Starting {0} request from {1}. {2}", request.GetType().Name, peerName,
			                concurrentRequestMsg);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(() => $"Request details: {request}");
			}

			if (requiresLicense && ! _licensed)
			{
				_licensed = await EnsureLicenseAsync();

				if (! _licensed)
				{
					throw new ConfigurationErrorsException(
						"No ArcGIS License could be initialized.");
				}
			}
		}

		private void EndRequest()
		{
			CurrentLoad?.EndRequest();

			if (CurrentLoad != null)
			{
				_msg.DebugFormat("Remaining requests that are inprogress: {0}",
				                 CurrentLoad.CurrentProcessCount);
			}

			if (Health?.IsAnyServiceUnhealthy() == true)
			{
				return;
			}

			if (SetUnhealthyAfterEachVerification)
			{
				ServiceUtils.SetUnhealthy(
					Health, GetType(),
					"Process is configured to be set to un-healthy after each request to allow for process recycling.");
			}

			if (UnhealthyMemoryLimitMegaBytes >= 0)
			{
				double privateBytesMb;
				using (Process process = Process.GetCurrentProcess())
				{
					long privateBytes = process.PrivateMemorySize64;

					const double mega = 1024 * 1024;

					privateBytesMb = privateBytes / mega;
				}

				if (privateBytesMb > UnhealthyMemoryLimitMegaBytes)
				{
					string reason =
						"High memory usage (allow for process recycling). " +
						$"Private Memory: {privateBytesMb} MB. Configured limit: {UnhealthyMemoryLimitMegaBytes} MB";

					ServiceUtils.SetUnhealthy(Health, GetType(), reason);
				}
				else
				{
					_msg.InfoFormat(
						"Process is still healthy in terms of memory usage. " +
						"Private Memory: {0} MB. Configured limit: {1} MB",
						privateBytesMb, UnhealthyMemoryLimitMegaBytes);
				}
			}
		}

		private async Task<bool> EnsureLicenseAsync()
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

			try
			{
				Task responseReaderTask = Task.Run(
					async () =>
					{
						// NOTE: The client should probably ensure that this call back
						//       does not exceed a certain time span. Ideally this does
						//       only block for a few seconds:

						// This results in an eternal loop and using a timespan here has no effect
						//while (resultData == null && elapsedMillis < timeOutMillis)
						{
							while (await requestStream.MoveNext().ConfigureAwait(false))
							{
								// TODO: only break if result_data.HasMoreDate is false
								resultData = requestStream.Current;
								break;
							}
						}
					});

				await responseStream.WriteAsync(r).ConfigureAwait(false);
				await responseReaderTask.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_msg.Warn("Error getting more data for class id " +
				          $"{r.DataRequest?.ClassDef?.ClassHandle}", e);
			}

			return resultData;
		}

		private static async Task<QueryDataRequest> RequestMoreDataAsync(
			IAsyncStreamReader<QueryDataRequest> requestStream,
			IServerStreamWriter<QueryDataResponse> responseStream,
			ServerCallContext context,
			QueryDataResponse r)
		{
			QueryDataRequest resultData = null;

			try
			{
				Task responseReaderTask = Task.Run(
					async () =>
					{
						// NOTE: The client should probably ensure that this call back
						//       does not exceed a certain time span. Ideally this does
						//       only block for a few seconds:

						while (await requestStream.MoveNext().ConfigureAwait(false))
						{
							// TODO: only break if result_data.HasMoreDate is false
							resultData = requestStream.Current;
							break;
						}
					});

				await responseStream.WriteAsync(r).ConfigureAwait(false);
				await responseReaderTask.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_msg.Warn("Error getting more data for class id " +
				          $"{r.DataRequest?.ClassDef?.ClassHandle}", e);
			}

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

			responseStreamer.CreateResponseAction =
				responseStreamer.CreateDataVerificationResponse;

			List<GdbObjRefMsg> deletableAllowedErrorRefs = new List<GdbObjRefMsg>();
			QualityVerification verification = null;
			string cancellationMessage = null;
			try
			{
				bool useStandaloneService =
					IsStandAloneVerification(request, initialRequest.Schema,
					                         out QualitySpecification specification);

				DistributedTestRunner distributedTestRunner = null;

				if (useStandaloneService)
				{
					// Stand-alone: Xml or specification list (WorkContextMsg is null!)
					verification = VerifyStandaloneXmlCore(
						specification, request.Parameters,
						distributedTestRunner, responseStreamer, trackCancel, true);
				}
				else
				{
					IBackgroundVerificationInputs backgroundVerificationInputs =
						CreateBackgroundVerificationInputs(request);

					if (initialRequest.Schema != null)
					{
						backgroundVerificationInputs.SetGdbSchema(
							ProtobufConversionUtils.CreateSchema(
								initialRequest.Schema.ClassDefinitions,
								initialRequest.Schema.RelclassDefinitions, moreDataRequest));
					}
					else if (moreDataRequest != null)
					{
						backgroundVerificationInputs.SetRemoteDataAccess(moreDataRequest);
					}

					responseStreamer.BackgroundVerificationInputs = backgroundVerificationInputs;

					qaService = CreateVerificationService(
						backgroundVerificationInputs, responseStreamer, trackCancel);

					verification = WithCulture(
						request.Parameters.ReportCultureCode,
						() => qaService.Verify(backgroundVerificationInputs, trackCancel));

					deletableAllowedErrorRefs.AddRange(
						GetDeletableAllowedErrorRefs(request.Parameters, qaService));
				}
			}
			catch (Exception e)
			{
				_msg.Error($"Error checking quality for request {request}", e);
				cancellationMessage = $"Server error: {ExceptionUtils.FormatMessage(e)}";

				ServiceUtils.SetUnhealthy(Health, GetType());
			}

			string cancelMessage = cancellationMessage ?? qaService?.CancellationMessage;

			ServiceCallStatus result = responseStreamer.SendFinalResponse(
				verification, cancelMessage, deletableAllowedErrorRefs,
				qaService?.GetVerifiedPerimeter(), trackCancel);

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
			responseStreamer.CreateResponseAction = responseStreamer.CreateVerificationResponse;

			List<GdbObjRefMsg> deletableAllowedErrorRefs = new List<GdbObjRefMsg>();
			QualityVerification verification = null;

			string cancellationMessage = null;

			DistributedTestRunner distributedTestRunner = null;

			try
			{
				bool useStandaloneService =
					IsStandAloneVerification(request, null, out QualitySpecification specification);

				if (DistributedProcessingClients != null && request.MaxParallelProcessing > 1)
				{
					distributedTestRunner =
						new DistributedTestRunner(DistributedProcessingClients, request)
						{
							QualitySpecification = specification,
							SupportedInstanceDescriptors = SupportedInstanceDescriptors
						};

					// TODO implement differently:
					string specName = request.Specification.XmlSpecification?
						.SelectedSpecificationName;
					int iSep = specName?.IndexOf(';') ?? -1;
					if (iSep >= 0)
					{
						string serviceConfigPath = specName.Substring(iSep + 1);
						if (File.Exists(serviceConfigPath))
						{
							XmlSerializer ser =
								new XmlSerializer(typeof(ParallelConfiguration));
							using (var r = new StreamReader(serviceConfigPath))
							{
								var config = (ParallelConfiguration) ser.Deserialize(r);
								distributedTestRunner.ParallelConfiguration = config;
							}
						}
					}
				}

				if (useStandaloneService)
				{
					if (distributedTestRunner != null)
					{
						// No re-harvesting in all the sub-verifications
						// TODO: Add SchemaMsg as DataModel property to standard request.
						// to make the intent more explicit!
						distributedTestRunner.SendModelsWithRequest = true;
					}

					// Stand-alone: Xml or specification list (WorkContextMsg is null!)
					verification = VerifyStandaloneXmlCore(
						specification, request.Parameters,
						distributedTestRunner, responseStreamer, trackCancel, true);
				}
				else
				{
					// DDX:
					verification = VerifyDdxQualityCore(request, distributedTestRunner,
					                                    responseStreamer, trackCancel,
					                                    out qaService);

					deletableAllowedErrorRefs.AddRange(
						GetDeletableAllowedErrorRefs(request.Parameters, qaService));
				}
			}
			catch (Exception e)
			{
				_msg.Error($"Error checking quality for request {request}", e);
				cancellationMessage = $"Server error: {ExceptionUtils.FormatMessage(e)}";

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					distributedTestRunner?.CancelSubverifications();

					ServiceUtils.SetUnhealthy(Health, GetType());
				}
			}

			ServiceCallStatus result = responseStreamer.SendFinalResponse(verification,
				cancellationMessage ?? qaService?.CancellationMessage, deletableAllowedErrorRefs,
				qaService?.GetVerifiedPerimeter(), trackCancel);

			return result;
		}

		private CancelableRequest RegisterRequest([CanBeNull] string requestUserName,
		                                          [CanBeNull] string environment,
		                                          CancellationToken token)
		{
			CancellationTokenSource combinedTokenSource =
				CancellationTokenSource.CreateLinkedTokenSource(token);

			return RequestAdmin?.RegisterRequest(requestUserName, environment, combinedTokenSource);
		}

		private ServiceCallStatus VerifyStandaloneXmlCore(
			StandaloneVerificationRequest request,
			VerificationProgressStreamer<StandaloneVerificationResponse> responseStreamer,
			ITrackCancel trackCancel)
		{
			SetupUserNameProvider(request.UserName);

			try
			{
				VerificationParametersMsg parameters = request.Parameters;

				// Currently no parallel processing for VerifyStandaloneXml() call. Use
				// VerifyQuality() with XML instead.
				DistributedTestRunner distributedTestRunner = null;

				QualitySpecification qualitySpecification;

				switch (request.SpecificationCase)
				{
					case StandaloneVerificationRequest.SpecificationOneofCase.XmlSpecification:
					{
						XmlQualitySpecificationMsg xmlSpecification = request.XmlSpecification;

						qualitySpecification = SetupQualitySpecification(xmlSpecification);
						break;
					}
					case StandaloneVerificationRequest.SpecificationOneofCase
					                                  .ConditionListSpecification:
					{
						ConditionListSpecificationMsg conditionListSpec =
							request.ConditionListSpecification;

						qualitySpecification = SetupQualitySpecification(conditionListSpec, null);
						break;
					}
					default: throw new ArgumentOutOfRangeException();
				}

				VerifyStandaloneXmlCore(qualitySpecification, parameters,
				                        distributedTestRunner, responseStreamer,
				                        trackCancel, false);
				return trackCancel.Continue()
					       ? ServiceCallStatus.Finished
					       : ServiceCallStatus.Cancelled;
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error during processing of request {0}", request);
				_msg.Error($"Error verifying quality: {ExceptionUtils.FormatMessage(e)}", e);

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					ServiceUtils.SetUnhealthy(Health, GetType());
				}

				return ServiceCallStatus.Failed;
			}

			// TODO: Final result message (error, warning count, row count with stop conditions, fulfilled)
		}

		private QualityVerification VerifyDdxQualityCore(
			VerificationRequest request,
			DistributedTestRunner distributedTestRunner,
			VerificationProgressStreamer<VerificationResponse> responseStreamer,
			ITrackCancel trackCancel,
			out BackgroundVerificationService qaService)
		{
			IBackgroundVerificationInputs backgroundVerificationInputs =
				CreateBackgroundVerificationInputs(request);

			responseStreamer.BackgroundVerificationInputs = backgroundVerificationInputs;

			BackgroundVerificationService service = CreateVerificationService(
				backgroundVerificationInputs, responseStreamer, trackCancel);

			service.DistributedTestRunner = distributedTestRunner;

			QualityVerification verification =
				WithCulture(request.Parameters.ReportCultureCode,
				            () => service.Verify(backgroundVerificationInputs, trackCancel));

			qaService = service;
			return verification;
		}

		private IBackgroundVerificationInputs CreateBackgroundVerificationInputs(
			VerificationRequest request)
		{
			// TODO: Separate long-lived objects, such as datasetLookup, domainTransactions (add to this class) from
			// short-term objects (request) -> add to background verification inputs
			IBackgroundVerificationInputs backgroundVerificationInputs =
				_verificationInputsFactoryMethod(request);

			if (SupportedInstanceDescriptors != null)
			{
				backgroundVerificationInputs.SupportedInstanceDescriptors =
					SupportedInstanceDescriptors;
			}

			return backgroundVerificationInputs;
		}

		private QualityVerification VerifyStandaloneXmlCore<T>(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] VerificationParametersMsg parameters,
			[CanBeNull] DistributedTestRunner distributedTestrunner,
			[NotNull] VerificationProgressStreamer<T> responseStreamer,
			ITrackCancel trackCancel,
			bool logToResponseStreamer) where T : class
		{
			// TODO: Remove parameter logToResponseStreamer once StandaloneXml service is removed

			IGeometry perimeter =
				ProtobufGeometryUtils.FromShapeMsg(parameters.Perimeter);

			var aoi = perimeter == null ? null : new AreaOfInterest(perimeter);

			_msg.DebugFormat("Provided perimeter: {0}", GeometryUtils.ToString(perimeter));

			XmlBasedVerificationService xmlService = new XmlBasedVerificationService(
				HtmlReportTemplatePath, QualitySpecificationTemplatePath);

			// NOTE: The report paths include the file names.
			xmlService.SetupOutputPaths(parameters.IssueFileGdbPath,
			                            parameters.VerificationReportPath,
			                            parameters.HtmlReportPath);

			xmlService.IssueFound +=
				(sender, args) => responseStreamer.AddPendingIssue(args);

			xmlService.DistributedTestRunner = distributedTestrunner;

			if (logToResponseStreamer)
			{
				xmlService.ProgressStreamer = responseStreamer;
			}

			DdxModel primaryModel =
				StandaloneVerificationUtils.GetPrimaryModel(qualitySpecification);
			responseStreamer.KnownIssueSpatialReference =
				primaryModel?.SpatialReferenceDescriptor?.GetSpatialReference();

			IssueRepositoryType issueRepositoryType = IssueRepositoryType.FileGdb;

			if (string.IsNullOrEmpty(parameters.IssueFileGdbPath))
			{
				xmlService.IssueRepositoryType = IssueRepositoryType.None;
			}
			else if (ExternalIssueRepositoryUtils.IssueRepositoryExists(
				         parameters.IssueFileGdbPath, IssueRepositoryType.FileGdb))
			{
				responseStreamer.Warning(
					$"The {issueRepositoryType} workspace '{parameters.IssueFileGdbPath}' already exists");

				return null;
			}

			xmlService.IssueRepositorySpatialReference =
				ProtobufGeometryUtils.FromSpatialReferenceMsg(
					parameters.IssueRepositorySpatialReference);

			WithCulture(parameters.ReportCultureCode,
			            () => xmlService.ExecuteVerification(qualitySpecification, aoi,
			                                                 parameters.TileSize, trackCancel));

			return xmlService.Verification;
		}

		private ServiceCallStatus QueryDataCore(
			[NotNull] QueryDataRequest request,
			IServerStreamWriter<QueryDataResponse> responseStream,
			Func<QueryDataResponse, QueryDataRequest> moreDataRequest,
			ITrackCancel trackCancel)
		{
			SetupUserNameProvider(request.UserName);

			try
			{
				// TODO: Allow trNoTransform with pass-through table name
				InstanceConfigurationMsg transformerConfigurationMsg = request.Transformer;

				IEnumerable<DataSourceMsg> dataSourceMsgs = request.DataSources;

				DataRequest dataRequest = request.DataRequest;

				//if (request.Schema != null)
				//{
				//	backgroundVerificationInputs.SetGdbSchema(
				//		ProtobufConversionUtils.CreateSchema(
				//			initialRequest.Schema.ClassDefinitions,
				//			initialRequest.Schema.RelclassDefinitions, moreDataRequest));
				//}
				//else if (moreDataRequest != null)
				//{
				//	backgroundVerificationInputs.SetRemoteDataAccess(moreDataRequest);
				//}

				TransformerConfiguration transformerConfiguration =
					CreateTransformerConfiguration(transformerConfigurationMsg, dataSourceMsgs,
					                               request.Schema, moreDataRequest);

				var datasetContext = new MasterDatabaseDatasetContext();

				ITableTransformer tableTransformer = InstanceFactory.CreateTransformer(
					transformerConfiguration,
					new SimpleDatasetOpener(datasetContext));

				foreach (IWorkspace workspace in tableTransformer.InvolvedTables.Select(
					         t => t.Workspace))
				{
					WorkspaceUtils.TryRefreshVersion(workspace);
				}

				// When querying the field name predictability is more important than not having dots in the names:
				if (tableTransformer is ITableTransformerFieldSettings transformerFieldSettings)
				{
					transformerFieldSettings.FullyQualifyFieldNames = true;
				}

				IReadOnlyTable table = (IReadOnlyTable) tableTransformer.GetTransformed();

				ITableFilter filter = VerificationRequestUtils.CreateFilter(
					table, dataRequest.SubFields, dataRequest.WhereClause,
					dataRequest.SearchGeometry);

				long maxRowCount = request.MaxRowCount > 0 ? request.MaxRowCount : 5000;
				foreach (GdbData resultBatch in VerificationRequestUtils.ReadGdbData(
					         table, filter, dataRequest.SubFields, -1, maxRowCount,
					         dataRequest.CountOnly))
				{
					var response = new QueryDataResponse
					               {
						               Data = resultBatch,
						               ServiceCallStatus =
							               resultBatch.HasMoreData
								               ? (int) ServiceCallStatus.Running
								               : (int) ServiceCallStatus.Finished
					               };

					_msg.DebugFormat("Sending message with {0} rows back to client...",
					                 resultBatch.GdbObjects.Count);

					if (! SendResponse(response, responseStream, trackCancel))
					{
						return ServiceCallStatus.Failed;
					}
				}

				return ServiceCallStatus.Finished;
			}
			catch (Exception e)
			{
				_msg.Error($"Error querying data for request {request}", e);
				_ = $"Server error: {ExceptionUtils.FormatMessage(e)}";

				// Always keep serving if query failed
				//if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				//{
				//	ServiceUtils.SetUnhealthy(Health, GetType());
				//}

				if (trackCancel?.Continue() == false || e.Message == "Already finished.")
				{
					// Typically: System.InvalidOperationException: Already finished.
					_msg.Debug(
						"The request has been cancelled and the client is already gone.", e);

					return ServiceCallStatus.Cancelled;
				}

				SendResponse(new QueryDataResponse
				             {
					             Message = new LogMsg()
					                       {
						                       Message =
							                       $"Server error: {ExceptionUtils.FormatMessage(e)}",
						                       MessageLevel = Level.Error.Value
					                       },
					             ServiceCallStatus = (int) ServiceCallStatus.Failed
				             }, responseStream, trackCancel);

				return ServiceCallStatus.Failed;
				//throw;
			}
		}

		private static bool SendResponse(QueryDataResponse response,
		                                 IServerStreamWriter<QueryDataResponse> responseStream,
		                                 ITrackCancel trackCancel)
		{
			try
			{
				responseStream.WriteAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				if (trackCancel?.Continue() == false || ex.Message == "Already finished.")
				{
					// Typically: System.InvalidOperationException: Already finished.
					_msg.Debug(
						"The verification has been cancelled and the client is already gone.",
						ex);

					return false;
				}

				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.Warn(
					"Error sending progress to the client. Retrying the last response in 1s...",
					ex);

				// Re-try (only for final message)
				Task.Delay(1000).Wait();
				responseStream.WriteAsync(response);
			}

			return true;
		}

		private static T WithCulture<T>(string cultureCode, Func<T> func)
		{
			if (string.IsNullOrEmpty(cultureCode))
			{
				return func();
			}

			var culture = new CultureInfo(cultureCode, false);

			return CultureInfoUtils.ExecuteUsing(culture, culture, func);
		}

		private bool IsStandAloneVerification([NotNull] VerificationRequest request,
		                                      [CanBeNull] SchemaMsg knownSchemaMsg,
		                                      out QualitySpecification qualitySpecification)
		{
			QualitySpecificationMsg specificationMsg = request.Specification;

			qualitySpecification = null;

			// Specific context such as project, work unit
			if (request.WorkContext?.Type > 0)
			{
				return false;
			}

			switch (specificationMsg.SpecificationCase)
			{
				case QualitySpecificationMsg.SpecificationOneofCase.XmlSpecification:
					XmlQualitySpecificationMsg xmlSpecification = specificationMsg.XmlSpecification;

					HashSet<int> excludedQcIds =
						new HashSet<int>(request.Specification.ExcludedConditionIds);

					qualitySpecification =
						SetupQualitySpecification(xmlSpecification, excludedQcIds);
					break;
				case QualitySpecificationMsg.SpecificationOneofCase.ConditionListSpecification:
					ConditionListSpecificationMsg conditionListSpec =
						specificationMsg.ConditionListSpecification;

					qualitySpecification =
						SetupQualitySpecification(conditionListSpec, knownSchemaMsg);
					break;
				default: return false;
			}

			QualitySpecificationUtils.DisableExcludedConditions(
				qualitySpecification, request.Specification.ExcludedConditionIds);

			return qualitySpecification != null;
		}

		private static QualitySpecification SetupQualitySpecification(
			[NotNull] XmlQualitySpecificationMsg xmlSpecification,
			[CanBeNull] ICollection<int> excludedConditionIds = null)
		{
			var dataSources = new List<DataSource>();

			// Initialize using valid data sources from XML:
			dataSources.AddRange(QualitySpecificationUtils.GetDataSources(xmlSpecification.Xml));

			if (dataSources.Count > 0)
			{
				_msg.InfoFormat("Initialized {0} data sources from XML.", dataSources.Count);
			}

			if (xmlSpecification.DataSourceReplacements.Count > 0)
			{
				foreach (string replacement in xmlSpecification.DataSourceReplacements)
				{
					AddOrReplace(replacement, dataSources);
				}
			}

			QualitySpecification qualitySpecification =
				QualitySpecificationUtils.CreateQualitySpecification(
					xmlSpecification.Xml, xmlSpecification.SelectedSpecificationName, dataSources,
					excludededConditionIds: excludedConditionIds);

			// ensure Xml- QualityConditionIds
			int nextQcId = 0;
			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				if (element.QualityCondition.Id == -1)
				{
					element.QualityCondition.SetCloneId(nextQcId);
				}

				nextQcId++;
			}

			return qualitySpecification;
		}

		private static void AddOrReplace([NotNull] string replacementString,
		                                 [NotNull] List<DataSource> dataSources)
		{
			List<string> replacementStrings =
				StringUtils.SplitAndTrim(replacementString, '|');
			Assert.AreEqual(2, replacementStrings.Count,
			                "Data source workspace is not of the format \"workspace_id | catalog_path\"");

			var dataSource =
				new DataSource(replacementStrings[0], replacementStrings[0])
				{
					WorkspaceAsText = replacementStrings[1]
				};

			// Replace existing data source from XML if it exists:
			int index = dataSources.FindIndex(ds => ds.ID == dataSource.ID);
			if (index >= 0)
			{
				dataSources[index] = dataSource;
				_msg.InfoFormat("Replaced data source <id> {0} with {1}", dataSource.ID,
				                dataSource);
			}
			else
			{
				dataSources.Add(dataSource);
				_msg.InfoFormat("Adding data source: {0}", dataSource);
			}
		}

		private QualitySpecification SetupQualitySpecification(
			[NotNull] ConditionListSpecificationMsg conditionsSpecificationMsg,
			[CanBeNull] SchemaMsg knownSchemaMsg)
		{
			if (SupportedInstanceDescriptors == null || SupportedInstanceDescriptors.Count == 0)
			{
				throw new InvalidOperationException(
					"No supported instance descriptors have been initialized.");
			}

			List<DataSource> dataSources =
				ProtobufQaUtils.GetDataSources(conditionsSpecificationMsg.DataSources);

			QualitySpecification qualitySpecification =
				CreateQualitySpecification(conditionsSpecificationMsg, dataSources, knownSchemaMsg);

			return qualitySpecification;
		}

		private QualitySpecification CreateQualitySpecification(
			[NotNull] ConditionListSpecificationMsg conditionsSpecificationMsg,
			[NotNull] ICollection<DataSource> dataSources,
			[CanBeNull] SchemaMsg knownSchemaMsg)
		{
			if (SupportedInstanceDescriptors == null || SupportedInstanceDescriptors.Count == 0)
			{
				throw new InvalidOperationException(
					"No supported instance descriptors have been initialized.");
			}

			ProtoBasedQualitySpecificationFactory factory =
				CreateSpecificationFactory(dataSources, SupportedInstanceDescriptors,
				                           knownSchemaMsg);

			QualitySpecification result = factory.CreateQualitySpecification(
				conditionsSpecificationMsg);

			return result;
		}

		private TransformerConfiguration CreateTransformerConfiguration(
			InstanceConfigurationMsg transformerConfigurationMsg,
			IEnumerable<DataSourceMsg> dataSourceMessages,
			SchemaMsg knownSchemaMsg,
			Func<QueryDataResponse, QueryDataRequest> moreDataRequest)
		{
			if (SupportedInstanceDescriptors == null || SupportedInstanceDescriptors.Count == 0)
			{
				throw new InvalidOperationException(
					"No supported instance descriptors have been initialized.");
			}

			List<DataSource> dataSources = ProtobufQaUtils.GetDataSources(dataSourceMessages);

			ProtoBasedQualitySpecificationFactory factory =
				CreateSpecificationFactory(dataSources, SupportedInstanceDescriptors,
				                           knownSchemaMsg, moreDataRequest);

			return factory.CreateTransformerConfiguration(transformerConfigurationMsg);
		}

		private static ProtoBasedQualitySpecificationFactory CreateSpecificationFactory(
			[NotNull] ICollection<DataSource> dataSources,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors,
			[NotNull] SchemaMsg knownSchemaMsg,
			[CanBeNull] Func<QueryDataResponse, QueryDataRequest> moreDataRequest)
		{
			var contextFactory = new MasterDatabaseWorkspaceContextFactory();

			IVerifiedModelFactory modelFactory =
				new ProtoBasedModelFactory(knownSchemaMsg, contextFactory);

			if (moreDataRequest == null)
			{
				// Data access via dataSources / model factory -> Review and adapt
				return new ProtoBasedQualitySpecificationFactory(modelFactory, dataSources,
				                                                 instanceDescriptors);
			}

			// Else: Data provided by client. Create virtual workspaces from provided SchemaMsg
			Assert.NotNull(moreDataRequest);

			Func<DataVerificationResponse, DataVerificationRequest> dataRequest = response =>
			{
				var input = new QueryDataResponse()
				            {
					            DataRequest = response.DataRequest,
					            ServiceCallStatus = (int) ServiceCallStatus.Running
				            };

				QueryDataRequest output = moreDataRequest(input);

				if (output?.InputData == null)
				{
					return null;
				}

				return new DataVerificationRequest
				       {
					       Data = output.InputData
				       };
			};

			IList<GdbWorkspace> workspaces = ProtobufConversionUtils.CreateSchema(
				knownSchemaMsg.ClassDefinitions,
				knownSchemaMsg.RelclassDefinitions, dataRequest);

			var modelsByWorkspaceId =
				new Dictionary<string, DdxModel>(StringComparer.OrdinalIgnoreCase);

			foreach (GdbWorkspace gdbWorkspace in workspaces)
			{
				foreach (DataSource dataSource in dataSources)
				{
					// TODO: Use DdxModelId instead of ID?
					if (! int.TryParse(dataSource.ID, out int modelId))
					{
						continue;
					}

					modelsByWorkspaceId.Add(dataSource.ID,
					                        modelFactory.CreateModel(gdbWorkspace,
						                        dataSource.DisplayName,
						                        modelId,
						                        dataSource.DatabaseName,
						                        dataSource.SchemaOwner));
				}
			}

			var factory = new ProtoBasedQualitySpecificationFactory(
				modelsByWorkspaceId, instanceDescriptors);

			return factory;
		}

		private static ProtoBasedQualitySpecificationFactory CreateSpecificationFactory(
			[NotNull] ICollection<DataSource> dataSources,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors,
			[CanBeNull] SchemaMsg knownSchemaMsg)
		{
			var contextFactory = new MasterDatabaseWorkspaceContextFactory();

			IVerifiedModelFactory modelFactory =
				knownSchemaMsg == null
					? (IVerifiedModelFactory) new VerifiedModelFactory(
						contextFactory, new SimpleVerifiedDatasetHarvester())
					: new ProtoBasedModelFactory(knownSchemaMsg, contextFactory);

			var factory =
				new ProtoBasedQualitySpecificationFactory(modelFactory, dataSources,
				                                          instanceDescriptors);

			return factory;
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
			var qaService = new BackgroundVerificationService(backgroundVerificationInputs)
			                {
				                ProgressStreamer = responseStreamer
			                };

			// TODO: Consider channeling all issues/progress through the above progress streamer
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

		private static void SetupUserNameProvider([NotNull] string userName)
		{
			_msg.DebugFormat("New verification request from {0}", userName);

			if (string.IsNullOrEmpty(userName))
			{
				return;
			}

			var perThreadProvider =
				EnvironmentUtils.GetUserNameProvider() as ThreadAffineUseNameProvider;

			Assert.NotNull(perThreadProvider, "No or unexpected type of user name provider");

			perThreadProvider.SetDisplayName(userName);
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

		private static void HandleCancellationException(object request,
		                                                ServerCallContext context,
		                                                TaskCanceledException canceledException)
		{
			_msg.VerboseDebug(() => $"Cancelled request: {request}");
			_msg.Debug($"Task cancelled: {context.CancellationToken.IsCancellationRequested}",
			           canceledException);
			_msg.Warn("Task was cancelled, likely by the client");
		}

		private static double GetUnhealthyMemoryLimit()
		{
			const string envVarServerSetUnhealthyMemory =
				"PROSUITE_QA_SERVER_UNHEALTHY_PROCESS_MEMORY";

			string envVarValue = Environment.GetEnvironmentVariable(
				envVarServerSetUnhealthyMemory);

			if (! string.IsNullOrEmpty(envVarValue))
			{
				if (double.TryParse(envVarValue, out double memoryLimitMb))
				{
					return memoryLimitMb;
				}

				_msg.WarnFormat("Cannot parse environment variable {0} value ({1})",
				                envVarServerSetUnhealthyMemory, envVarValue);
			}

			return -1;
		}
	}
}
