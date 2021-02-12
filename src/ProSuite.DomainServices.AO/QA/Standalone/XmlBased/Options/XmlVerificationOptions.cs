using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	[XmlRoot("VerificationOptions",
	         Namespace = "urn:EsriDE.ProSuite.QA.XmlBasedVerificationOptions-1.0")]
	public class XmlVerificationOptions
	{
		[XmlElement("KeyFields")]
		[CanBeNull]
		public XmlKeyFields KeyFields { get; set; }

		[XmlElement("IssueGdbName")]
		[CanBeNull]
		public string IssueGdbName { get; set; }

		[XmlElement("ExceptionGdbName")]
		[CanBeNull]
		public string ExceptionGdbName { get; set; }

		[XmlElement("MxdDocumentName")]
		[CanBeNull]
		public string MxdDocumentName { get; set; }

		[XmlElement("XmlReportName")]
		[CanBeNull]
		public string XmlReportName { get; set; }

		[XmlElement("DefaultTemplateDirectoryPath")]
		[CanBeNull]
		public string DefaultTemplateDirectoryPath { get; set; }

		[XmlArray("HtmlReports")]
		[XmlArrayItem("Report")]
		[CanBeNull]
		public List<XmlHtmlReportOptions> HtmlReports { get; set; }

		[XmlArray("IssueMaps")]
		[XmlArrayItem("IssueMap")]
		[CanBeNull]
		public List<XmlIssueMapOptions> IssueMaps { get; set; }

		[XmlArray("QualitySpecificationReports")]
		[XmlArrayItem("Report")]
		[CanBeNull]
		public List<XmlSpecificationReportOptions> SpecificationReports { get; set; }

		[XmlElement("Exceptions")]
		[CanBeNull]
		public XmlExceptionConfiguration Exceptions { get; set; }
	}
}
