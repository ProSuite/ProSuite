using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	public class XmlLinearNetwork
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("description")]
		[DefaultValue(null)]
		public string Description { get; set; }

		public XmlNamedEntity ModelReference { get; set; }

		[XmlArrayItem("NetworkDataset")]
		public List<XmlLinearNetworkDataset> NetworkDatasets { get; set; }

		[XmlAttribute("enforceFlowDirection")]
		public bool EnforceFlowDirection { get; set; }

		[XmlAttribute("customTolerance")]
		public double CustomTolerance { get; set; }
	}

	public class XmlLinearNetworkDataset
	{
		[XmlAttribute(AttributeName = "dataset")]
		public string Dataset { get; set; }

		[XmlAttribute(AttributeName = "WhereClause")]
		public string WhereClause { get; set; }

		[XmlAttribute(AttributeName = "splitting")]
		public bool Splitting { get; set; }

		[XmlAttribute(AttributeName = "isDefaultJunction")]
		public bool IsDefaultJunction { get; set; }

		[XmlAttribute(AttributeName = "model")]
		public string Model { get; set; }
	}
}
