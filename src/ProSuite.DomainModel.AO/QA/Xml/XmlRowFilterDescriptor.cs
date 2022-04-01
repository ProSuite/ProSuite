using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlRowFilterDescriptor : XmlDescriptor
	{
		[XmlElement("RowFilterClass")]
		[CanBeNull]
		public XmlClassDescriptor RowFilterClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}
}
