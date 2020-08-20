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


		public IEnumerable<ProSuiteQAServerConfiguration> DefaultQAServiceConfig
		{
			get
			{
				return new List<ProSuiteQAServerConfiguration>() {
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
						//ServiceConnection = @"c:\git\PRD_ProSuite\py_esrich_prosuite_qa_gpservice\ArcGISPro\ProSuiteToolbox.pyt"
						ServiceConnection = ""
					};

				case ProSuiteQAServiceType.GPService:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPService,
						ServiceName = @"QAGPServices\ProSuiteQAService",
						//ServiceConnection = @"C:\Users\algr\Documents\ArcGIS\Projects\test\admin on vsdev2414.esri-de.com_6443 (3).ags"
						ServiceConnection = ""
					};
				default:
					return new ProSuiteQAServerConfiguration();
			}
		}

	}
}
