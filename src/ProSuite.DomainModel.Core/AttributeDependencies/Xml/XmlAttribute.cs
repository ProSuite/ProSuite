using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public class XmlAttribute
	{
		[XmlAttribute("name")]
		public string Name { get; set; }
	}
}
