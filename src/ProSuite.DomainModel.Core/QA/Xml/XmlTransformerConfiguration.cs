using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlTransformerConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("transformerDescriptor")]
		public string TransformerDescriptorName { get; set; }
	}
}
