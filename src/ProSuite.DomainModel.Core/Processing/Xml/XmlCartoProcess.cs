using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public class XmlCartoProcess
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("description")]
		[DefaultValue(null)]
		public string Description { get; set; }

		public XmlNamedEntity ModelReference { get; set; }

		public XmlNamedEntity TypeReference { get; set; }

		[XmlArrayItem("Parameter")]
		public List<XmlCartoProcessParameter> Parameters { get; } =
			new List<XmlCartoProcessParameter>();
	}
}
