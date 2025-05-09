using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlDataSourceDescription
	{
		[XmlAttribute("workspaceName")]
		public string WorkspaceName { get; set; }

		[XmlAttribute("description")]
		public string Description { get; set; }

		[UsedImplicitly]
		public XmlDataSourceDescription() { }

		public XmlDataSourceDescription(string workspaceName, string description)
		{
			WorkspaceName = workspaceName;
			Description = description;
		}
	}
}
