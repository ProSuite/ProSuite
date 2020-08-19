using ProSuite.Commons.QA.ServiceManager.Types;
using System;
using System.Threading.Tasks;

namespace ProSuite.Commons.QA.ServiceManager.Interfaces
{
    // QA service provider can be Microservices (local or server), GP (local or server), ...
    public interface IProSuiteQAServiceProvider
    {
        ProSuiteQAServiceType ServiceType { get; }

        Task<ProSuiteQAResponse> StartQAAsync(ProSuiteQARequest request);
        ProSuiteQAResponse StartQASync(ProSuiteQARequest request);

        event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		void UpdateConfig(ProSuiteQAServerConfiguration serviceConfig);
	}
}
