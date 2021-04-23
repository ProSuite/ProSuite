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

		[XmlArray("PreProcessors")]
		[XmlArrayItem(typeof(XmlInstanceConfiguration), ElementName = "PreProcessor")]
		[DefaultValue(null)]
		public List<XmlInstanceConfiguration> PreProcessors { get; set; }

		[XmlArray("TableTransformers")]
		[XmlArrayItem(typeof(XmlInstanceConfiguration), ElementName = "TableTransformer")]
		[DefaultValue(null)]
		public List<XmlInstanceConfiguration> TableTransformers { get; set; }

	}
}
