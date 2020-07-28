using ProSuite.Commons.QA.ServiceManager;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using ProSuite.Commons.QA.ServiceProviderArcGIS;
using ProSuite.Commons.QA.SpecificationProviderFile;
using System.Collections.Generic;


namespace QAConfigurator
{
    public class QAConfiguration
    {
		//public event EventHandler<ProSuiteConfigurationEventArgs> OnConfigurationChanged;

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

		public static ProSuiteQAManager QAManager
		{
			get
			{
				return new ProSuiteQAManager(
					new List<IProSuiteQAServiceProvider>() {
						new QAServiceProviderGP(GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService)),
						new QAServiceProviderGP(GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal))
					},
					new QASpecificationProviderXml()
					);
			}
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

		}
	}
}
