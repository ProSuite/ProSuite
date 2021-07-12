using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProSuite.QA.ServiceManager
{
	public class ProSuiteQAManager
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// QA service provider - GP, microservices, mock, REST, ...
		IEnumerable<IProSuiteQAServiceProvider> _serviceProviders { get; set; }

		// QA specifications provider - XML, DDX, ....
		IQualitySpecificationReferencesProvider _specificationsProvider { get; set; }

		public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		public ProSuiteQAManager(IEnumerable<IProSuiteQAServiceProvider> availableServices,
		                         IQualitySpecificationReferencesProvider specificationsProvider)
		{
			_serviceProviders = availableServices;
			foreach (var service in _serviceProviders)
				service.OnStatusChanged += Service_OnStatusChanged;

			_specificationsProvider = specificationsProvider;
		}

		public IEnumerable<IProSuiteQAServiceProvider> ServiceProviders
		{
			get
			{
				return _serviceProviders;
			}
		}

		public IQualitySpecificationReferencesProvider SpecificationProvider => _specificationsProvider;

		private void Service_OnStatusChanged(object sender, ProSuiteQAServiceEventArgs e)
		{
			_msg.Info($"{e.State}: {e.Data}");
			OnStatusChanged?.Invoke(this, e);
		}

		public void OnConfigurationChanged(object sender, ProSuiteQAConfigEventArgs e)
		{
			var serviceConfigs = (IEnumerable<ProSuiteQAServerConfiguration>)e?.Data;

			if (serviceConfigs != null)
			{
				_msg.Info("QA service providers config actualized");
				UpdateServiceConfigs(serviceConfigs);
			}
		}

		public async Task<ProSuiteQAResponse> StartQATestingAsync(ProSuiteQARequest request)
		{
			var service = GetQAService(request.ServiceType);
			if (service == null)  // throw? 
				return new ProSuiteQAResponse()
				{
					Error = ProSuiteQAError.ServiceUnavailable
				};

			// service is responsible for correct format (passtrough)
			return await service.StartQAAsync(request);
		}

		public ProSuiteQAResponse StartQATesting(ProSuiteQARequest request)
		{
			var service = GetQAService(request.ServiceType);
			return service?.StartQASync(request);
		}

		private IProSuiteQAServiceProvider GetQAService(ProSuiteQAServiceType type)
		{
			// TODO find free service? 
			return _serviceProviders.FirstOrDefault(sp => sp.ServiceType == type);
		}

		private void UpdateServiceConfigs(IEnumerable<ProSuiteQAServerConfiguration> serviceConfigs)
		{
			foreach (var serviceConfig in serviceConfigs)
			{
				var serviceProvider = GetQAService(serviceConfig.ServiceType);
				if (serviceProvider != null)
					serviceProvider.UpdateConfig(serviceConfig);
			}
		}

		public async Task<string> GetQASpecificationsConnection(string currentQASpecificationName)
		{
			var spec = await _specificationsProvider?.GetQualitySpecification(currentQASpecificationName);
			return spec.Connection;
		}
	}

}
