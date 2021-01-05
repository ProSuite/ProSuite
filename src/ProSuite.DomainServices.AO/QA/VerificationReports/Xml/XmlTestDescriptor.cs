using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlTestDescriptor
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("Description")]
		public string Description { get; set; }

		[XmlAttribute("factory")]
		public string FactoryClass { get; set; }

		[XmlAttribute("class")]
		public string TestClass { get; set; }

		[XmlAttribute("constructor")]
		public string TestConstructorIndex { get; set; }

		[XmlAttribute("assembly")]
		public string Assembly { get; set; }
	}
}
