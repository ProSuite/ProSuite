using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlDatasetTestParameterValue : XmlTestParameterValue
	{
		[XmlAttribute("where")]
		[DefaultValue(null)]
		public string WhereClause { get; set; }

		[XmlAttribute("usedAsReferenceData")]
		[DefaultValue(false)]
		public bool UsedAsReferenceData { get; set; }

		[XmlAttribute("workspace")]
		[DefaultValue(null)]
		public string WorkspaceId { get; set; }

		[XmlArrayItem(ElementName = "RowFilterName")] 
		[DefaultValue(null)]
		public List<string> RowFilterNames { get; set; }
	}
}
