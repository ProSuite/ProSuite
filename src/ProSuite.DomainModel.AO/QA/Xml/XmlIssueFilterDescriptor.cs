using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlIssueFilterDescriptor : XmlDescriptor
	{
		[XmlElement("IssueFilterClass")]
		[CanBeNull]
		public XmlClassDescriptor IssueFilterClass
		{
			get => ClassDescriptor;
			set => ClassDescriptor = value;
		}
	}
}
