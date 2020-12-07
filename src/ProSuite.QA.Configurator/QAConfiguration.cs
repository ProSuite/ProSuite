using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using ProSuite.QA.ServiceProviderArcGIS;
using ProSuite.QA.SpecificationProviderFile;
using System.Collections.Generic;

namespace ProSuite.QA.Configurator
{
	public class QAConfiguration
	{
		private static QAConfiguration _configuration;
		public static QAConfiguration Current
		{
			get { return _configuration ?? (_configuration = new QAConfiguration()); }
		}

		public IEnumerable<IProSuiteQAServiceProvider> GetQAServiceProviders(IEnumerable<ProSuiteQAServerConfiguration> serverConfigs)
		{
			var listOfQAServiceProviders = new List<IProSuiteQAServiceProvider>();
			if (serverConfigs == null) return listOfQAServiceProviders;

			foreach (var serverConfig in serverConfigs)
			{
				if (serverConfig.ServiceType == ProSuiteQAServiceType.GPLocal ||
					serverConfig.ServiceType == ProSuiteQAServiceType.GPService)
				{
					listOfQAServiceProviders.Add(new QAServiceProviderGP(serverConfig));
				}
			}
			return listOfQAServiceProviders;
		}

		public IQASpecificationProvider GetQASpecificationsProvider(ProSuiteQASpecificationsConfiguration specConfig)
		{
			return new QASpecificationProviderXml();
		}

		public IEnumerable<ProSuiteQAServerConfiguration> DefaultQAServiceConfig
		{
			get
			{
				return new List<ProSuiteQAServerConfiguration>
				       {
					       GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService),
					       GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal)
				       };
			}
		}

		public ProSuiteQASpecificationsConfiguration DefaultQASpecConfig
		{
			get
			{
				return new ProSuiteQASpecificationsConfiguration();
			}
		}

		public ProSuiteQAServerConfiguration GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType serviceType)
		{
			switch (serviceType)
			{
				case ProSuiteQAServiceType.GPLocal:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPLocal,
						ServiceName = @"QAGPLocal",
						ServiceConnection = @"c:\git\PRD_ProSuite\py_esrich_prosuite_qa_gpservice\ArcGISPro\ProSuiteToolbox.pyt"
					};

				case ProSuiteQAServiceType.GPService:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPService,
						ServiceName = @"QAGPServices\ProSuiteQAService",
						ServiceConnection = ""
					};
				default:
					return new ProSuiteQAServerConfiguration();
			}
		}
	}
}
