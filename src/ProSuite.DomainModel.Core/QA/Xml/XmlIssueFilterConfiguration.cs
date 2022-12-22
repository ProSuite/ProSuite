using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlIssueFilterConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("issueFilterDescriptor")]
		public string IssueFilterDescriptorName { get; set; }
	}
}
