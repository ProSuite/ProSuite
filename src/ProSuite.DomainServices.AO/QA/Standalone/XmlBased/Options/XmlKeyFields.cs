using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlKeyFields
	{
		[CanBeNull]
		[XmlAttribute("defaultKeyField")]
		public string DefaultKeyField { get; set; }

		[CanBeNull]
		[XmlElement("DataSource")]
		public List<XmlDataSourceKeyFields> DataSourceKeyFields { get; set; }
	}
}
