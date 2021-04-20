using System.Threading.Tasks;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using NUnit.Framework;
using ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps;

namespace ProSuite.Microservices.Server.AO.Test
{
	public class ServiceHealthTest
	{
		[Test]
		public void CanSetServiceHealthStatus()
		{
			HealthServiceImpl healthImpl = new HealthServiceImpl();

			IServiceHealth health = new ServiceHealth(healthImpl);

			var serviceName = "RemoveOverlapsGrpc";

			health.SetStatus(serviceName, true);

			Task<HealthCheckResponse> response = healthImpl.Check(
				new HealthCheckRequest() {Service = serviceName},
				null);

			Assert.IsFalse(health.IsAnyServiceUnhealthy());

			Assert.AreEqual(HealthCheckResponse.Types.ServingStatus.Serving,
			                response.Result.Status);

			// Set the status through the type
			health.SetStatus(typeof(RemoveOverlapsGrpcImpl), false);

			response = healthImpl.Check(
				new HealthCheckRequest() {Service = serviceName},
				null);

			Assert.AreEqual(HealthCheckResponse.Types.ServingStatus.NotServing,
			                response.Result.Status);

			Assert.IsTrue(health.IsAnyServiceUnhealthy());
		}

		[Test]
		public void CanSetEmptyServiceNameHealth()
		{
			HealthServiceImpl healthImpl = new HealthServiceImpl();

			IServiceHealth health = new ServiceHealth(healthImpl);

			health.SetStatus(string.Empty, true);

			Task<HealthCheckResponse> response = healthImpl.Check(
				new HealthCheckRequest(), null);

			Assert.AreEqual(HealthCheckResponse.Types.ServingStatus.Serving,
			                response.Result.Status);

			Assert.IsFalse(health.IsAnyServiceUnhealthy());
		}
	}
}
