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

			Init();
		}

		private void Init()
		{
			// TODO more than one service?
			_serviceClient = new VerifyQualityService.VerifyQualityServiceClient(ClientChannel);
		}

		// TODO return value
		public async Task CallServer(ProSuiteGrpcServerRequest request, CancellationTokenSource cancellationTokenSource)
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

					var serviceCall = _serviceClient.PerformQualityVerification(qualityRequest, null, null, cancellationTokenSource.Token);
					while (await serviceCall.ResponseStream.MoveNext())
					{
						if (cancellationTokenSource.IsCancellationRequested)
							break;

						var response = serviceCall.ResponseStream.Current;
						OnServiceResponseReceived?.Invoke(this,
							new ProSuiteGrpcEventArgs(
								new ProSuiteGrpcServerResponse
								{
									RequestType = request.ServiceType,
									ResponseMessage = $"{response.Status} {response.StepsDone * 100/response.StepsTotal}%",
									ResponseData = serviceCall.ResponseStream.Current
								}));
					}
					break;
				default:
					return; // ??
			}

		}

		public void Dispose()
		{
			Stop();
		}

		public void Stop()
		{
			ClientChannel.ShutdownAsync().Wait();
		}
	}
}
