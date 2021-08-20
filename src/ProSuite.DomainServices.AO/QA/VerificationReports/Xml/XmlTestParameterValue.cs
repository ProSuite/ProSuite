using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlTestParameterValue
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("value")]
		public string Value { get; set; }

		[XmlAttribute("dataset")]
		public string Dataset { get; set; }

		[XmlAttribute("whereClause")]
		[DefaultValue(null)]
		public string WhereClause { get; set; }

		[XmlAttribute("usedAsReferenceData")]
		[DefaultValue(false)]
		public bool UsedAsReferenceData { get; set; }

		[XmlElement("valueSource")]
		public XmlInstanceConfiguration ValueSource { get; set; }

		[XmlArray("rowFilters")]
		[XmlArrayItem("rowFilter")]
		public List<XmlInstanceConfiguration> RowFilters { get; set; }
	}
}
