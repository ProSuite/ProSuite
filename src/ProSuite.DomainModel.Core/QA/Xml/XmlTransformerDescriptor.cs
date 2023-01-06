using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlTransformerDescriptor : XmlInstanceDescriptor
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
