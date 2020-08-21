using ProSuite.QA.ServiceManager.Interfaces;
using System.Collections.Generic;

namespace ProSuite.QA.SpecificationProviderFile
{
	// read QA specification from XML file(s) 
	public class QASpecificationProviderXml : IQASpecificationProvider
	{
		public IList<string> GetQASpecificationNames()
		{
			return new List<string> { "MCTest", "OSM_all", "OSM" };
		}
	}
}
