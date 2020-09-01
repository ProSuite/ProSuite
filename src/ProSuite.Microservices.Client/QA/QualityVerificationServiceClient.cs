using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient : MicroserviceClientBase
	{
		public QualityVerificationGrpc.QualityVerificationGrpcClient QaClient { get; }

		public QualityVerificationServiceClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig)
		{
			QaClient = new QualityVerificationGrpc.QualityVerificationGrpcClient(Channel);
		}

		protected override string ServiceName =>
			Assert.NotNull(QaClient.GetType()).DeclaringType?.Name;
	}
}
