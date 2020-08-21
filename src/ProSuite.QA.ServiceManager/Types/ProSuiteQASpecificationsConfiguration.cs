using System.Collections.Generic;

namespace ProSuite.QA.ServiceManager.Types
{
	public class ProSuiteQASpecificationsConfiguration
	{
		public ProSuiteQASpecificationProviderType SpecificationProviderType { get; set; }
		public IList<string> XmlSpecificationsList { get; set; }
	}
}
