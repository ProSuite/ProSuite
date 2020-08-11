using ArcGIS.Desktop.Core;
//using QAServiceManager.Types;
using System.Collections.Generic;

namespace Clients.AGP.ProSuiteSolution
{
    public class ProSuiteQAProjectItem : CustomProjectItemBase
    {
        //public ProSuiteQAServiceType ServiceType { get; set; }

        public string ServiceName { get; set; }

        public string ServerConnection { get; set; }

        //public ProSuiteQASpecificationProviderType SpecificationProviderType { get; set; }

        public IList<string> XmlSpecificationsList { get; set; }

        public override ProjectItemInfo OnGetInfo()
        {
            return new ProjectItemInfo() { Name = "QAProjectItem" };
        }
    }

}
