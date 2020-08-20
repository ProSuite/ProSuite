using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.Commons.QA.ServiceManager.Types
{
	public class ProSuiteQASpecificationsConfiguration
	{
		public ProSuiteQASpecificationProviderType SpecificationProviderType { get; set; }
		public IList<string> XmlSpecificationsList { get; set; }
	}
}
