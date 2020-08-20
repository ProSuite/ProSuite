using ProSuite.Commons.QA.ServiceManager.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.QA.SpecificationProviderFile
{
	// read QA specification from XML file(s) 
	public class QASpecificationProviderXml : IQASpecificationProvider
	{
		public IList<string> GetQASpecificationNames()
		{
			return new List<String>() { "MCTest", "OSM_all", "OSM" };
		}
	}
}
