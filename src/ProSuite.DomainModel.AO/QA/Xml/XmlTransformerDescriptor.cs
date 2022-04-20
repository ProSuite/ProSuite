using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlTransformerDescriptor : XmlDescriptor
	{
		[XmlElement("TransformerClass")]
		[CanBeNull]
		public XmlClassDescriptor TransformerClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}
}
