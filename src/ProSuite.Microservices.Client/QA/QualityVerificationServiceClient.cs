using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using EsriDE.Commons.Microservices.AO;
//using EsriDE.ProSuite.Processing.Evaluation;
using Grpc.Core;
//using Grpc.Core;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using Swisstopo.Topgis.Core;
using Swisstopo.Topgis.Domain.Workflow;
using Swisstopo.Topgis.Microservices;
using Swisstopo.Topgis.Services.QA;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ProSuite.Commons;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient : MicroserviceClientBase
	{
		private readonly string _localServerExecutable = @"tg_microserver_qa.exe";
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public QualityVerificationGrpc.QualityVerificationGrpcClient QaClient { get; }

		public QualityVerificationServiceClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig)
		{
			QaClient = new QualityVerificationGrpc.QualityVerificationGrpcClient(Channel);
			if (channelConfig.HostName == "localhost")
				StartLocalServer(); // this could be started later ...
		}

		protected override string ServiceName =>
			Assert.NotNull(QaClient.GetType()).DeclaringType?.Name;

		private void StartLocalServer()
		{
			string executablePath =
				ConfigurationUtils.GetExecutablePath(_localServerExecutable);
			AllowStartingLocalServer(executablePath);
		}

		//public BackgroundVerificationResult QualityVerificationResult { get; private set; }

		//public IQualityVerificationProgressTracker Progress { get; private set; }

		//public IClientIssueMessageCollector IssueMessageCollector { get; private set; }

		public async Task<QualityVerification> VerifyServerRequest(VerificationRequest verificationRequest,
													  IDomainTransactionManager domainTransactions,
		                                              IQualityVerificationRepository qualityVerificationRepository,
		                                              IQualityConditionRepository qualityConditionRepository,
		                                              IClientIssueMessageCollector resultIssueCollector,
		                                              CancellationTokenSource cancellationTokenSource)
		{
			
			//Progress = new QualityVerificationProgressTracker
			//           {
			//	           CancellationTokenSource = cancellationTokenSource
			//           };

			//QualityVerificationResult = new BackgroundVerificationResult(
			//	IssueMessageCollector, domainTransactions, qualityVerificationRepository,
			//	qualityConditionRepository);

			var verificationRun =
				new BackgroundVerificationRun(domainTransactions, qualityVerificationRepository,
				                              qualityConditionRepository, cancellationTokenSource)
				{
					ResultIssueCollector = resultIssueCollector
					//,SaveAction = SaveAction
					//,ShowReportAction = ShowReportAction
				};

			try
			{
				ServiceCallStatus result = await verificationRun.ExecuteAndProcessMessagesAsync(
					                           QaClient, verificationRequest);
				_msg.InfoFormat(
					"Service call result: {0} ({1} errors, {2} warnings)", result,
					verificationRun.Progress.ErrorCount, verificationRun.Progress.WarningCount);

				// TODO: Same as in TopgisEnvImpl! Add verified conditions!
				verificationRun.QualityVerificationResult?.SaveIssues(
					ErrorDeletionInPerimeter.VerifiedQualityConditions);


			}
			catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
			{
				System.Console.WriteLine(
					"Cancel notification has been sent to the service. Client already shutting down...");
			}
			catch (Exception e)
			{
				System.Console.WriteLine(e);
			}
			return null;
		}

		// TODO - generic request in MicroservicesBase?
		public QualityVerification RequestServerVerify( VerificationRequest request, ITrackCancel trackCancel)
		{
			//// Found on the internet: https://stackoverflow.com/questions/14526377/why-does-this-async-action-hang
			//int result = Task.Run(async () =>
			//		 await ExecuteAndProcessGpMessagesAsync(rpcClient, paramValuesProto, gpMessages)
			//).Result; // dont deadlock anymore

			var cancellationTokenSource = new CancellationTokenSource();

			Task<QualityVerification> task = Task.Run(
				async () => await ExecuteAndProcessMessagesAsync(
					            request, cancellationTokenSource));

			while (!task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
			{
				if (trackCancel != null && !trackCancel.Continue())
				{
					// Cancel on the server ...
					cancellationTokenSource.Cancel();

					// .. and continue waiting until the server returns (it might have to finish
					// properly) because it has already posted changes.
				}

				Thread.Sleep(50);
			}

			QualityVerification result = null;
			if (!task.IsFaulted && !task.IsCanceled)
			{
				result = task.GetAwaiter().GetResult();
			}

			if (result == null && trackCancel != null && !trackCancel.Continue())
			{
				_msg.Warn("Quality verification was cancelled");
				return null;
			}

			return result;
		}

		// TODO - generic?
		private async Task<QualityVerification> ExecuteAndProcessMessagesAsync( VerificationRequest request, CancellationTokenSource cancellationTokenSource)
		{
			AsyncServerStreamingCall<VerificationResponse> call = QaClient.VerifyQuality(request);

			VerificationResponse responseMsg = null;

			while (await call.ResponseStream.MoveNext(cancellationTokenSource.Token))
			{
				responseMsg = call.ResponseStream.Current;

				LogProgressMessage(new ProgressMsg()
				                   {
					                   Message = responseMsg.Progress.Message,
									   ProgressTotalSteps = responseMsg.Progress.OverallProgressTotalSteps,
									   ProgressCurrentStep = responseMsg.Progress.OverallProgressCurrentStep
									   //MessageLevel = Level.Info.Value
				}); 
			}

			QualityVerification result = null;

			if (responseMsg?.QualityVerification != null)
			{
				foreach (var qualityVerificationCondition in responseMsg.QualityVerification.ConditionVerifications)
				{
				}

				result = new QualityVerification(
					responseMsg.QualityVerification.SpecificationId,
					responseMsg.QualityVerification.SpecificationName,
					responseMsg.QualityVerification.SpecificationDescription,
					"user",
					new List<QualityConditionVerification>());
			}

			return result;
		}

		//public VerificationRequest CreateVerificationRequest(
		//	WorkContextMsg workContextMsg,
		//	QualitySpecificationMsg qualitySpecificationMsg,
		//	IGeometry perimeter = null)
		//{
		//	var request = new VerificationRequest();

		//	request.WorkContext = workContextMsg;
		//	request.Specification = qualitySpecificationMsg;
		//	request.Parameters = new VerificationParametersMsg();

		//	if (perimeter != null && !perimeter.IsEmpty)
		//	{
		//		ShapeMsg areaOfInterest = ProtobufConversionUtils.ToShapeMsg(perimeter);
		//		request.Parameters.Perimeter = areaOfInterest;
		//	}
		//	//request.UserName = EnvironmentUtils.UserDisplayName;

		//	return request;
		//}


		//private async Task<bool> RunQaRpcAsync(
		//	MicroserviceQaClientArguments arguments,
		//	CancellationTokenSource cancellationTokenSource)
		//{
		//	//Func<AsyncServerStreamingCall<VerificationResponse>> verificationFunc =
		//	//	GetVerificationFunc(arguments);

		//	//QualityVerificationGrpc.QualityVerificationGrpcClient qaClient =
		//	//	GetWorkUnitVerificationGrpc(arguments.HostName, arguments.Port);

		//	VerificationRequest request = CreateRequest(arguments);

		//	//IDomainTransactionManager domainTransactions =
		//	//	new StatelessDomainTransactionManager(UnitOfWork.Instance);
		//	//IDatasetLookup datasetLookup = new GlobalDatasetLookup(
		//	//	domainTransactions, Ddx.Datasets, Ddx.Associations,
		//	//	null);

		//	ClientIssueMessageCollector clientIssueCollector = null;

		//	if (arguments.SaveErrorsInVerifiedContext)
		//	{
		//		clientIssueCollector = GetIssueMessageClientRepository(
		//			arguments, request, datasetLookup, domainTransactions);
		//	}

		//	BackgroundVerificationRun verificationRun =
		//		new BackgroundVerificationRun(
		//			domainTransactions, Ddx.QualityVerifications, Ddx.QualityConditions,
		//			cancellationTokenSource)
		//		{
		//			ResultIssueCollector = clientIssueCollector
		//		};

		//	//var progressTracker = new QualityVerificationProgressTracker();
		//	//progressTracker.CancellationTokenSource = cancellationTokenSource;

		//	try
		//	{
		//		ServiceCallStatus result = await verificationRun.ExecuteAndProcessMessagesAsync(
		//									   QaClient, request);

		//		//_msg.InfoFormat(
		//		//	"Service call result: {0} ({1} errors, {2} warnings)", result,
		//		//	verificationRun.Progress.ErrorCount, verificationRun.Progress.WarningCount);

		//		// TODO: Same as in TopgisEnvImpl! Add verified conditions!
		//		verificationRun.QualityVerificationResult?.SaveIssues(
		//			ErrorDeletionInPerimeter.VerifiedQualityConditions);
		//	}
		//	catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
		//	{
		//		System.Console.WriteLine(
		//			"Cancel notification has been sent to the service. Client already shutting down...");
		//	}
		//	catch (Exception e)
		//	{
		//		System.Console.WriteLine(e);
		//	}

		//	//_msg.InfoFormat("Verification Status: {0}", verificationRun.Progress.RemoteCallStatus);
		//	//_msg.InfoFormat("Error count: {0}", verificationRun.Progress.ErrorCount);

		//	return true;
		//}
	}
}
