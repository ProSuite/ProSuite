using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.GrpcCore.QA;
using ProSuite.Microservices.Definitions.QA.Test;

namespace ProSuite.Microservices.Client.GrpcCore.QualityTestService
{
	public class ExternalTestClient : GrpcCoreQualityVerificationServiceClient
	{
		private QualityTestGrpc.QualityTestGrpcClient _staticTestClient;

		public ExternalTestClient([NotNull] string url)
			: base(ClientChannelConfig.Parse(url)) { }

		public ExternalTestClient([NotNull] string host,
		                          int port = 5151,
		                          bool useTls = false,
		                          string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		public override string ServiceName => nameof(QualityTestGrpc);

		public override string ServiceDisplayName => "Quality Test Service";

		protected override void ChannelOpenedCore(ChannelBase channel)
		{
			_staticTestClient = new QualityTestGrpc.QualityTestGrpcClient(channel);
		}

		[CanBeNull]
		public QualityTestGrpc.QualityTestGrpcClient TestClient => _staticTestClient;
	}
}
