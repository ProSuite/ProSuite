using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlFilter
	{
		[XmlAttribute("issueFilterName")]
		public string IssueFilterName { get; set; }
	}
}
