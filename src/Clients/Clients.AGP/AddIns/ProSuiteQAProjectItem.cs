using ArcGIS.Desktop.Core;
using ProSuite.Commons.QA.ServiceManager.Types;
using System.Collections.Generic;

namespace Clients.AGP.ProSuiteSolution
{
    public class ProSuiteQAProjectItem : CustomProjectItemBase
    {
		public ProSuiteQAProjectItem()
		{
			ServerConfigurations = new List<ProSuiteQAServerConfiguration>()
			{
				GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal),
				GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService)
			};
			SpecificationConfiguration = new ProSuiteQASpecificationsConfiguration();
		}

		public ProSuiteQAProjectItem(IEnumerable<ProSuiteQAServerConfiguration> serverConfigurations, ProSuiteQASpecificationsConfiguration specifationConfiguration)
		{
			SpecificationConfiguration = specifationConfiguration;
			ServerConfigurations = serverConfigurations;
		}

		public ProSuiteQASpecificationsConfiguration SpecificationConfiguration { get; set; }

		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }

        public override ProjectItemInfo OnGetInfo()
        {
            return new ProjectItemInfo() { Name = "QAProjectItem" };
        }

		private ProSuiteQAServerConfiguration GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType serviceType)
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
