using ProSuite.Commons.QA.ServiceManager;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using ProSuite.Commons.QA.ServiceProviderArcGIS;
using ProSuite.Commons.QA.SpecificationProviderFile;
using System;
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

		//public event EventHandler<ProSuiteConfigurationEventArgs> OnConfigurationChanged;

		private List<ProSuiteQAServerConfiguration> _serviceConfigurations = null;
		public List<ProSuiteQAServerConfiguration> QAServiceConfigurations {
			get
			{
				if (_serviceConfigurations == null)
				{
					_serviceConfigurations = new List<ProSuiteQAServerConfiguration>() {
						GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService),
						GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal)
					};
				}
				return _serviceConfigurations;
			}
			set
			{
				_serviceConfigurations = value;
				// TODO Notify others than config is changed
				//Invoke(OnConfigurationChanged);
			}
		}

		public static ProSuiteQAManager QAManager
		{
			get
			{
				return new ProSuiteQAManager( GetQAServiceProviders(),	new QASpecificationProviderXml());
			}
		}

		private static List<IProSuiteQAServiceProvider> GetQAServiceProviders()
		{
			var listOfQAServiceProviders = new List<IProSuiteQAServiceProvider>();

			// check if service provider is allowed?

			var localServerConfiguration = Current.QAServiceConfigurations.Find(c => (c.ServiceType == ProSuiteQAServiceType.GPLocal));
			if(localServerConfiguration != null)
				listOfQAServiceProviders.Add(new QAServiceProviderGP(localServerConfiguration));

			var gpServerConfiguration = Current.QAServiceConfigurations.Find(c => (c.ServiceType == ProSuiteQAServiceType.GPService));
			if (gpServerConfiguration != null)
				listOfQAServiceProviders.Add(new QAServiceProviderGP(gpServerConfiguration));

			return listOfQAServiceProviders;
		}

		private static ProSuiteQAServerConfiguration GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType serviceType)
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
						ServiceConnection = @"C:\Users\algr\Documents\ArcGIS\Projects\test\admin on vsdev2414.esri-de.com_6443 (3).ags"
					};
				default:
					return new ProSuiteQAServerConfiguration();
			}
		}

		void UpdateConfiguration()
		{
			// TODO algr: update ServiceConfiguration and inform service providers

		}
	}
}
