using System.Collections.Generic;

namespace ProSuite.QA.ServiceManager.Types
{
	public class ProSuiteQASpecificationsConfiguration
	{
		public ProSuiteQASpecificationProviderType SpecificationProviderType { get; set; }
		public string SpecificationsProviderConnection { get; set; }

		public ProSuiteQASpecificationsConfiguration(ProSuiteQASpecificationProviderType specificationProviderType, string specificationsProviderConnection)
		{
			SpecificationProviderType = specificationProviderType;
			SpecificationsProviderConnection = specificationsProviderConnection;
		}
	}
}
