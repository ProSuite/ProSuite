using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class XmlInvolvedObjectsMatchCriterionIgnoredDatasets
	{
		[CanBeNull]
		[XmlAttribute("name")]
		public string ModelName { get; set; }

		[CanBeNull]
		[XmlElement("DatasetName")]
		public List<string> DatasetNames { get; set; }
	}
}
