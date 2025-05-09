using System.Collections.Generic;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.Microservices.Client.GrpcCore.QA
{
	public class GrpcCoreQualityVerificationServiceClient : QualityVerificationServiceClientBase
	{
		private const string _localhost = "localhost";

		public GrpcCoreQualityVerificationServiceClient([NotNull] string host = _localhost,
		                                                int port = 5151,
		                                                bool useTls = false,
		                                                string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		public GrpcCoreQualityVerificationServiceClient(
			[NotNull] IClientChannelConfig channelConfig)
			: base(channelConfig) { }

		public GrpcCoreQualityVerificationServiceClient(
			[NotNull] IList<IClientChannelConfig> channelConfigs)
			: base(channelConfigs) { }

		#region Overrides of QualityVerificationServiceClientBase

		protected override IQualityVerificationClient CreateClient(
			string hostName, int port, bool useTls,
			string clientCertificate)
		{
			return new GrpcCoreQualityVerificationServiceClient(
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
			Channel channel = GrpcUtils.CreateChannel(
				host, port, credentials, maxMessageLength);

			return channel;
		}

		public override string GetChannelState()
		{
			Channel channel = Channel as Channel;

			return channel?.State.ToString() ?? "<unknown>";
		}

		public override string GetAddress()
		{
			Channel channel = Channel as Channel;

			return GrpcUtils.GetAddress(channel, ChannelServiceName);
		}

		#endregion
	}
}
