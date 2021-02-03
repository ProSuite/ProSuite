using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlInvolvedObjectsMatchCriteria
	{
		[CanBeNull]
		[XmlArray("IgnoredDatasets")]
		[XmlArrayItem("DataSource")]
		public List<XmlInvolvedObjectsMatchCriterionIgnoredDatasets> IgnoredDatasets { get; set; }
	}
}