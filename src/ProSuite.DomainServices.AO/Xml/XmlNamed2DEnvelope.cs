using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.Xml
{
	public class XmlNamed2DEnvelope : Xml2DEnvelope
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("description")]
		public string Description { get; set; }
	}
}
