using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlDataSourceKeyFields
	{
		[CanBeNull]
		[XmlAttribute("name")]
		public string ModelName { get; set; }

		[CanBeNull]
		[XmlAttribute("defaultKeyField")]
		public string DefaultKeyField { get; set; }

		[CanBeNull]
		[XmlElement("Dataset")]
		public List<XmlDatasetKeyField> DatasetKeyFields { get; set; }
	}
}