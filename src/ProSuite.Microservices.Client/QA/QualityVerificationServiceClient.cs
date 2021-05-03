using System;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient : MicroserviceClientBase
	{
		private QualityVerificationGrpc.QualityVerificationGrpcClient _staticQaClient;

		public QualityVerificationServiceClient([NotNull] ClientChannelConfig channelConfig)
			: base(channelConfig) { }

		public QualityVerificationServiceClient([NotNull] string host,
		                                        int port = 5151,
		                                        bool useTls = false,
		                                        string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		protected override string ServiceName => nameof(QualityVerificationGrpc);

		[CanBeNull]
		public QualityVerificationGrpc.QualityVerificationGrpcClient QaClient
		{
			get
			{
				if (_staticQaClient != null)
				{
					return _staticQaClient;
				}

				if (ChannelIsLoadBalancer)
				{
					ChannelCredentials credentials =
						GrpcUtils.CreateChannelCredentials(UseTls, ClientCertificate);

					var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

					Channel actualChannel = TryGetChannelFromLoadBalancer(
						Channel, credentials, ServiceName,
						enoughForLargeGeometries);

					if (actualChannel == null)
					{
						// TODO: Cycle through other client configs
						throw new InvalidOperationException(
							"Load balancer has not provided a valid channel");
					}

					return new QualityVerificationGrpc.QualityVerificationGrpcClient(actualChannel);
				}

				throw new InvalidOperationException(
					"Neither a static channel nor a load balancer channel has been opened.");
			}
		}

		protected override void ChannelOpenedCore(Channel channel)
		{
			if (! ChannelIsLoadBalancer)
			{
				_staticQaClient =
					new QualityVerificationGrpc.QualityVerificationGrpcClient(channel);
			}
		}
	}
}
