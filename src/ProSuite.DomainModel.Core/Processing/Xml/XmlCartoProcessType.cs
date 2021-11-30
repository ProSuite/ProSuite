using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public class XmlCartoProcessType
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		public XmlClassDescriptor ClassDescriptor { get; set; }

		[XmlAttribute("description")]
		[DefaultValue(null)]
		public string Description { get; set; }
	}
}
