using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO
{
	public interface IServiceHealth
	{
		void SetStatus([NotNull] string serviceName, bool serving);

		void SetStatus([NotNull] Type serviceType, bool serving);

		bool IsAnyServiceUnhealthy();
	}
}
