using Grpc.Core;
using ProSuite.ProtobufClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProSuite.GrpcClient
{
	public class ProSuiteGrpcClient : IDisposable
	{
		private string _serverAddress;
		private int _port;

		public EventHandler<ProSuiteGrpcEventArgs> OnServiceResponseReceived;

		private Channel _clientChannel = null;
		public Channel ClientChannel
		{
			get => _clientChannel ?? (_clientChannel = new Channel(_serverAddress, _port, ChannelCredentials.Insecure));
		}

		private VerifyQualityService.VerifyQualityServiceClient _serviceClient;

		public ProSuiteGrpcClient(string serverAddress, int port)
		{
			_serverAddress = serverAddress;
			_port = port;

			InitServices();
		}

		private void InitServices()
		{
			// TODO more than one service?
			_serviceClient = new VerifyQualityService.VerifyQualityServiceClient(ClientChannel);
		}

		// TODO return value
		public async Task CallServer(ProSuiteGrpcServerRequest request, CancellationToken cancellationToken)
		{
			// TODO check client connectivity (health?)
			// TODO background thread?
			// TODO deadline

			switch (request.ServiceType)
			{
				case ProSuiteGrpcServiceType.VerifyQuality:

					// TODO convert from/to protobufs format because of references 
				
					var qualityRequest = new VerifyQualityRequest
					{
						RequestId = 1,
						RequestType = VerifyQualityRequestType.Xml
					};

					var serviceCall = _serviceClient.PerformQualityVerification(qualityRequest, null, null, cancellationToken);
					while (await serviceCall.ResponseStream.MoveNext())
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						var response = serviceCall.ResponseStream.Current;
						OnServiceResponseReceived?.Invoke(this,
							new ProSuiteGrpcEventArgs(
								new ProSuiteGrpcServerResponse
								{
									RequestType = request.ServiceType,
									Status = ParseStatus(response.Status),
									ResponseMessage = $"{response.Status} {response.StepsDone * 100/response.StepsTotal}%",
									ResponseData = serviceCall.ResponseStream.Current
								}));
					}
					break;
				default:
					return; // ??
			}

		}

		private ProSuiteGrpcServerResponseStatus ParseStatus(VerifyQualityResponse.Types.ProgressStatus status)
		{
			switch (status)
			{
				case VerifyQualityResponse.Types.ProgressStatus.Done:
					return ProSuiteGrpcServerResponseStatus.Done;
				case VerifyQualityResponse.Types.ProgressStatus.Failed:
					return ProSuiteGrpcServerResponseStatus.Failed;
				case VerifyQualityResponse.Types.ProgressStatus.Info:
					return ProSuiteGrpcServerResponseStatus.Info;
				case VerifyQualityResponse.Types.ProgressStatus.Progress:
					return ProSuiteGrpcServerResponseStatus.Progress;
			}
			return ProSuiteGrpcServerResponseStatus.Other;
		}

		public void Dispose()
		{
			ClientChannel.ShutdownAsync().Wait();
		}
	}
}
