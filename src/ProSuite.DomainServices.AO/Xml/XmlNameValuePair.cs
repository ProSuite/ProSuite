using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.Xml
{
	public class XmlNameValuePair
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("value")]
		public string Value { get; set; }
	}
}
