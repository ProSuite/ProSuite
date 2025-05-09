using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlWorkspaceDescription
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public HtmlWorkspaceDescription(XmlDataSourceDescription xmlDataSourceDescription)
		{
			Name = xmlDataSourceDescription.WorkspaceName;
			Description = xmlDataSourceDescription.Description;
		}
	}
}
