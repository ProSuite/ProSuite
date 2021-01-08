using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client
{
	[UsedImplicitly]
	public class ClientChannelConfig
	{
		public string HostName { get; set; }

		public int Port { get; set; }

		public bool UseTls { get; set; }

		[CanBeNull]
		public string ClientCertificate { get; set; }
	}
}
