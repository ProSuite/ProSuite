using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlRowFilterConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("rowFilterDescriptor")]
		public string RowFilterDescriptorName { get; set; }
	}
}
