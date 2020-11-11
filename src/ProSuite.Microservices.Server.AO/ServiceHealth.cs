using System;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Microservices.Server.AO
{
	public class ServiceHealth : IServiceHealth
	{
		private readonly HealthServiceImpl _health;

		private bool _isUnhealthy;

		public ServiceHealth(HealthServiceImpl health)
		{
			_health = health;
		}

		public void SetStatus(string serviceName, bool serving)
		{
			var status = serving
				             ? HealthCheckResponse.Types.ServingStatus.Serving
				             : HealthCheckResponse.Types.ServingStatus.NotServing;

			_health.SetStatus(serviceName, status);

			if (status == HealthCheckResponse.Types.ServingStatus.NotServing)
			{
				_isUnhealthy = true;
			}
		}

		public void SetStatus(Type serviceType, bool serving)
		{
			string serviceName = Assert.NotNull(serviceType.BaseType).DeclaringType?.Name;

			SetStatus(Assert.NotNull(serviceName), serving);
		}

		public bool IsAnyServiceUnhealthy()
		{
			return _isUnhealthy;
		}
	}
}
