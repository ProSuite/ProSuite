using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using ProSuite.QA.ServiceProviderArcGIS;
using ProSuite.QA.SpecificationProviderFile;
using System.Collections.Generic;
using System.IO;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.QA.Configurator
{
	public class QAConfiguration
	{
		private static QAConfiguration _configuration;
		public static QAConfiguration Current
		{
			get { return _configuration ?? (_configuration = new QAConfiguration()); }
		}

		// TODO common utils, ...
		private readonly string _qaInstallationsFolder = @"c:\ProSuite\ArcGisPro\";
		private IQualitySpecificationReferencesProvider _specificationProvider;

		public void SetupGrpcConfiguration(IQualityVerificationEnvironment verificationEnvironment)
		{
			_specificationProvider = new QaSpecificationProviderGrpc(verificationEnvironment);
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

		public IQualitySpecificationReferencesProvider GetQASpecificationsProvider(ProSuiteQASpecificationsConfiguration specConfig)
		{
			if (_specificationProvider != null)
			{
				return _specificationProvider;
			}

			return new QASpecificationProviderXml(specConfig.SpecificationsProviderConnection);
		}

		public IEnumerable<ProSuiteQAServerConfiguration> DefaultQAServiceConfig =>
			new List<ProSuiteQAServerConfiguration>
			{
				GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService),
				GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal)
			};

		public ProSuiteQASpecificationsConfiguration DefaultQASpecConfig =>
			new ProSuiteQASpecificationsConfiguration(ProSuiteQASpecificationProviderType.Xml,
			                                          Path.Combine(_qaInstallationsFolder, "Specifications"));

		public ProSuiteQAServerConfiguration GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType serviceType)
		{
			switch (serviceType)
			{
				case ProSuiteQAServiceType.GPLocal:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPLocal,
						ServiceName = @"QAGPLocal",
						ServiceConnection = Path.Combine(_qaInstallationsFolder, @"Toolbox\ProSuiteToolbox.pyt")
					};

				case ProSuiteQAServiceType.GPService:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPService,
						ServiceName = @"QAGPServices\ProSuiteQAService",
						ServiceConnection = "",
						DefaultOutputFolder = @"\\vsdev2414\prosuite_server_trials\results"
			};
				default:
					return new ProSuiteQAServerConfiguration();
			}
		}
	}
}
