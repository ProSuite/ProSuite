using Grpc.Core;
using ProSuite.ProtobufClasses;
using ProSuite.QA.ServiceManager;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Threading.Tasks;

namespace ProSuite.GrpcServer
{
	class VerifyQualityGrpcService : VerifyQualityService.VerifyQualityServiceBase
	{
		private readonly IQualityVerificationAgent _verifyAgent;

		public VerifyQualityGrpcService(IQualityVerificationAgent verifyAgent)
		{
			_verifyAgent = verifyAgent;
		}

		public override async Task PerformQualityVerification(
			VerifyQualityRequest request,
			IServerStreamWriter<VerifyQualityResponse> responseStream,
			ServerCallContext context)
		{
			// TODO check if done?

			EventHandler<ProSuiteQAServiceEventArgs> verifyAgentOnStatusChanged =
				async (sender, args) => await SendMessage(responseStream, ProSuiteQAServiceState.Progress, args );
		
			EventHandler<ProSuiteQAServiceEventArgs> verifyAgentOnCompleted =
				async (sender, args) => await SendMessage(responseStream, ProSuiteQAServiceState.Finished, args);

			EventHandler<ProSuiteQAServiceEventArgs> verifyAgentOnError =
				async (sender, args) => await SendMessage(responseStream, ProSuiteQAServiceState.Failed, args);

			_verifyAgent.OnStatusChanged += verifyAgentOnStatusChanged;
			_verifyAgent.OnCompleted += verifyAgentOnCompleted;
			_verifyAgent.OnError += verifyAgentOnError;

			try
			{
				_verifyAgent.DoQualityVerification(
					new ProSuiteQARequest(ProSuiteQAServiceType.gRPC));
			}
			catch (Exception ex)
			{
				await responseStream.WriteAsync(
					VerifyMessageBuilder.CreateServerVerifyResponse(new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Failed, ex.Message)));
			}
			finally
			{
				_verifyAgent.OnStatusChanged -= verifyAgentOnStatusChanged;
				_verifyAgent.OnCompleted -= verifyAgentOnCompleted;
				_verifyAgent.OnError -= verifyAgentOnError;
			}
		}

		private async Task SendMessage(IServerStreamWriter<VerifyQualityResponse> responseStream, ProSuiteQAServiceState progress, ProSuiteQAServiceEventArgs args)
		{
			if (responseStream != null)
			{
				Console.WriteLine("Response sent");
				await responseStream.WriteAsync(
					VerifyMessageBuilder.CreateServerVerifyResponse(args));
			}
		}
	}


	public static class VerifyMessageBuilder
	{
		// from 
		public static VerifyQualityResponse CreateServerVerifyResponse(ProSuiteQAServiceEventArgs eventArgs)
		{
			var resp = eventArgs.Data as ProSuiteQAResponse;
			if (resp == null)
				return new VerifyQualityResponse() { Status = VerifyQualityResponse.Types.ProgressStatus.Failed };

			// TODO temporary - payload of events should be revisited and simplified !!!! 
			if (resp.Error != ProSuiteQAError.None)
				return new VerifyQualityResponse() { Status = VerifyQualityResponse.Types.ProgressStatus.Failed, Result = new VerifyQualityResult()};

			var serverResponse=
				new VerifyQualityResponse()
                {
					Status = ParseStatus(eventArgs.State)
                };
			return serverResponse;
		}

		private static VerifyQualityResponse.Types.ProgressStatus ParseStatus(ProSuiteQAServiceState state)
		{
			switch (state)
			{
				case ProSuiteQAServiceState.Progress:
					return VerifyQualityResponse.Types.ProgressStatus.Progress;
				case ProSuiteQAServiceState.Finished:
					return VerifyQualityResponse.Types.ProgressStatus.Done;
				case ProSuiteQAServiceState.Info:
					return VerifyQualityResponse.Types.ProgressStatus.Info;
			}
			return VerifyQualityResponse.Types.ProgressStatus.Failed;
		}

	}
}


