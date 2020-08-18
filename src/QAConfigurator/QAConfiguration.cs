using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using ProSuite.Commons.QA.ServiceProviderArcGIS;
using ProSuite.Commons.QA.SpecificationProviderFile;
using System.Collections.Generic;


namespace QAConfigurator
{
	public class QAConfiguration
    {

		private static QAConfiguration _configuration = null;
		public static QAConfiguration Current
		{
			get
			{
				if (_configuration == null)
				{
					_configuration = new QAConfiguration();
				}
				return _configuration;
			}
		}

		public IEnumerable<IProSuiteQAServiceProvider> GetQAServiceProviders(IEnumerable<ProSuiteQAServerConfiguration> serverConfigs)
		{
			var listOfQAServiceProviders = new List<IProSuiteQAServiceProvider>();
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

	}
}
