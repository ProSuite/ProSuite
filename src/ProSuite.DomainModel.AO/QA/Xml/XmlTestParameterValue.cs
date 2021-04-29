using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlTestParameterValue
	{
		[XmlAttribute("parameter")]
		public string TestParameterName { get; set; }

		[XmlAttribute("value")]
		[DefaultValue(null)]
		public string Value { get; set; }

		[XmlAttribute("instanceConfigurationName")] // TODO : rename?
		[DefaultValue(null)]
		public string InstanceConfigurationName { get; set; }
	}

}
