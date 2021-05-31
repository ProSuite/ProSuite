using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO
{
	public class StartedGrpcServer<T> where T : class
	{
		public StartedGrpcServer([NotNull] Grpc.Core.Server server,
		                         [NotNull] T serviceImplementation,
		                         [CanBeNull] IServiceHealth serviceHealth)
		{
			Server = server;
			ServiceImplementation = serviceImplementation;
			ServiceHealth = serviceHealth;
		}

		[NotNull]
		public Grpc.Core.Server Server { get; }

		[CanBeNull]
		public IServiceHealth ServiceHealth { get; }

		[NotNull]
		public T ServiceImplementation { get; }
	}
}
