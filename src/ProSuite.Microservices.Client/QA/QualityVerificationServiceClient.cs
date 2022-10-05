using System;
using System.Collections.Generic;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient : MicroserviceClientBase
	{
		private QualityVerificationGrpc.QualityVerificationGrpcClient _staticQaClient;
		private QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient _staticDdxClient;

		public QualityVerificationServiceClient([NotNull] ClientChannelConfig channelConfig)
			: base(channelConfig) { }

		public QualityVerificationServiceClient([NotNull] IList<ClientChannelConfig> channelConfigs)
			: base(channelConfigs) { }

		public QualityVerificationServiceClient([NotNull] string host,
		                                        int port = 5151,
		                                        bool useTls = false,
		                                        string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		public override string ServiceName => nameof(QualityVerificationGrpc);

		public override string ServiceDisplayName => "Quality Verification";

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
					Channel actualChannel = GetBalancedChannel();

					return new QualityVerificationGrpc.QualityVerificationGrpcClient(actualChannel);
				}

				throw new InvalidOperationException(
					"Neither a static channel nor a load balancer channel has been opened.");
			}
		}

		[CanBeNull]
		public QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient DdxClient
		{
			get
			{
				if (_staticDdxClient != null)
				{
					return _staticDdxClient;
				}

				if (ChannelIsLoadBalancer)
				{
					Channel actualChannel = GetBalancedChannel();

					return new QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient(
						actualChannel);
				}

				throw new InvalidOperationException(
					"Neither a static channel nor a load balancer channel has been opened.");
			}
		}

		protected override void ChannelOpenedCore(Channel channel)
		{
			// In case of fail-over from a fixed address to a load-balancer:
			if (ChannelIsLoadBalancer)
			{
				_staticQaClient = null;
				_staticDdxClient = null;
			}
			else
			{
				_staticQaClient =
					new QualityVerificationGrpc.QualityVerificationGrpcClient(channel);
				_staticDdxClient =
					new QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient(channel);
			}
		}

		private Channel GetBalancedChannel()
		{
			ChannelCredentials credentials =
				GrpcUtils.CreateChannelCredentials(UseTls, ClientCertificate);

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			Channel actualChannel = TryGetChannelFromLoadBalancer(
				Channel, credentials, ServiceName,
				enoughForLargeGeometries);

			if (actualChannel == null)
			{
				if (TryOpenOtherChannel())
				{
					actualChannel = TryGetChannelFromLoadBalancer(
						Channel, credentials, ServiceName,
						enoughForLargeGeometries);
				}
				else
				{
					throw new InvalidOperationException(
						"Load balancer has not provided a valid channel");
				}
			}

			return actualChannel;
		}
	}
}
