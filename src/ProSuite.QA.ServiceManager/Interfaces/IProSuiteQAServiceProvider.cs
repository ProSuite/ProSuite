using ProSuite.QA.ServiceManager.Types;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProSuite.QA.ServiceManager.Interfaces
{
    // QA service provider can be Microservices (local or server), GP (local or server), ...
    public interface IProSuiteQAServiceProvider
    {
        ProSuiteQAServiceType ServiceType { get; }

        Task<ProSuiteQAResponse> StartQAAsync(ProSuiteQARequest request, CancellationToken token);
        ProSuiteQAResponse StartQASync(ProSuiteQARequest request, CancellationToken token);

        event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		void UpdateConfig(ProSuiteQAServerConfiguration serviceConfig);
	}
}
