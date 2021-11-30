using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public class XmlCartoProcessParameter
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("value")]
		public string Value { get; set; }
	}
}
