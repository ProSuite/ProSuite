using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO
{
	public class StartedGrpcServer
	{
		public StartedGrpcServer([NotNull] Grpc.Core.Server server,
		                         [CanBeNull] IServiceHealth serviceHealth)
		{
			Server = server;
			ServiceHealth = serviceHealth;
		}

		[NotNull]
		public Grpc.Core.Server Server { get; }

		[CanBeNull]
		public IServiceHealth ServiceHealth { get; }
	}
}
