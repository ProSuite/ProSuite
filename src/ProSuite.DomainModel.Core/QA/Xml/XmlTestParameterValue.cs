using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlTestParameterValue
	{
		[XmlAttribute("parameter")]
		public string TestParameterName { get; set; }

		[XmlAttribute("value")]
		[DefaultValue(null)]
		public string Value { get; set; }

		[XmlAttribute("transformerName")]
		[DefaultValue(null)]
		public string TransformerName { get; set; }
	}
}
