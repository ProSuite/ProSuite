using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Client;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.Microservices.Client.GrpcNet
{
	public class GrpcNetQualityVerificationServiceClient : QualityVerificationServiceClientBase
	{
		private const string _localhost = "localhost";

		public GrpcNetQualityVerificationServiceClient([NotNull] string host = _localhost,
		                                               int port = 5151,
		                                               bool useTls = false,
		                                               string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		public GrpcNetQualityVerificationServiceClient(
			[NotNull] IClientChannelConfig channelConfig)
			: base(channelConfig) { }

		public GrpcNetQualityVerificationServiceClient(
			[NotNull] IList<IClientChannelConfig> channelConfigs)
			: base(channelConfigs) { }

		#region Overrides of QualityVerificationServiceClientBase

		protected override IQualityVerificationClient CreateClient(
			string hostName, int port, bool useTls,
			string clientCertificate)
		{
			return new GrpcNetQualityVerificationServiceClient(
				hostName, port, useTls, clientCertificate);
		}

		#endregion

		#region Overrides of MicroserviceClientBase

		protected override ChannelBase OpenChannelCore(
			string host,
			int port,
			ChannelCredentials credentials,
			int maxMessageLength)
		{
			ChannelBase channel = GrpcUtils.CreateChannel(
				host, port, credentials, maxMessageLength);

			return channel;
		}

		public override string GetChannelState()
		{
			GrpcChannel channel = Channel as GrpcChannel;

			return channel?.State.ToString() ?? "<unknown>";
		}

		public override string GetAddress()
		{
			GrpcChannel channel = (GrpcChannel) Channel;

			return GrpcUtils.GetAddress(channel, ChannelServiceName);
		}

		#endregion
	}
}
