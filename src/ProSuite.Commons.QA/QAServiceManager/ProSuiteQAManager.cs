using ProSuite.Commons.Logging;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ProSuite.Commons.QA.ServiceManager
{

    public class ProSuiteQAManager
    {
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// QA service provider - GP, microservices, mock, REST, ...
		IList<IProSuiteQAServiceProvider> _serviceProviders { get; set; }

        // QA specifications provider - XML, DDX, ....
        IQASpecificationProvider _specificationsProvider { get; set; }

        public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

        public ProSuiteQAManager(IList<IProSuiteQAServiceProvider> availableServices, IQASpecificationProvider specificationsProvider)
        {
            _serviceProviders = availableServices;
            foreach(var service in _serviceProviders)
                service.OnStatusChanged += Service_OnStatusChanged;

            _specificationsProvider = specificationsProvider;
        }

        private void Service_OnStatusChanged(object sender, ProSuiteQAServiceEventArgs e)
        {
            //_msg.Info($"QAGPServiceProvider: {e.State}");
            OnStatusChanged?.Invoke(this, e);
        }

        public IList<string> GetSpecificationNames()
        {
            return _specificationsProvider?.GetQASpecificationNames();
        }

        public async Task<ProSuiteQAResponse> StartQATestingAsync(ProSuiteQARequest request)
        {
            var service = GetQAService(request.ServiceType);
            if (service == null)  // throw? 
                return new ProSuiteQAResponse() {
                    Error = ProSuiteQAError.ServiceUnavailable
                };

            // service is responsible for correct format (passtrough)
            return await service?.StartQAAsync(request);
        }

        public ProSuiteQAResponse StartQATesting(ProSuiteQARequest request)
        {
            var service = GetQAService(request.ServiceType);
            return service?.StartQASync(request);
        }

        private IProSuiteQAServiceProvider GetQAService(ProSuiteQAServiceType type)
        {
            // TODO find free service? 
            return _serviceProviders.Where(sp => sp.ServiceType == type).FirstOrDefault();
        }

    }
}