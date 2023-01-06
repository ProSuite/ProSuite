using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlIssueFilterDescriptor : XmlInstanceDescriptor
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
