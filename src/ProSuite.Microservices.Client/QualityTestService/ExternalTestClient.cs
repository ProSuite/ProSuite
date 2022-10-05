using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA.Test;

namespace ProSuite.Microservices.Client.QualityTestService
{
	public class ExternalTestClient : MicroserviceClientBase
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

		protected override void ChannelOpenedCore(Channel channel)
		{
			_staticTestClient = new QualityTestGrpc.QualityTestGrpcClient(channel);
		}

		[CanBeNull]
		public QualityTestGrpc.QualityTestGrpcClient TestClient => _staticTestClient;
	}
}
