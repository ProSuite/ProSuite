using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient : MicroserviceClientBase
	{
		[CanBeNull]
		public QualityVerificationGrpc.QualityVerificationGrpcClient QaClient { get; private set; }

		public QualityVerificationServiceClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig) { }

		protected override string ServiceName => QaClient?.GetType().DeclaringType?.Name;

		protected override void ChannelOpenedCore(Channel channel)
		{
			QaClient = new QualityVerificationGrpc.QualityVerificationGrpcClient(channel);
		}
	}
}
