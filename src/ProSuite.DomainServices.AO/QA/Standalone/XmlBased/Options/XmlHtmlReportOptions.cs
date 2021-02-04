using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlHtmlReportOptions
	{
		[CanBeNull]
		[XmlAttribute("template")]
		public string TemplatePath { get; set; }

		[CanBeNull]
		[XmlAttribute("reportFile")]
		public string ReportFileName { get; set; }

		[CanBeNull]
		[XmlArray("DataQualityCategories")]
		[XmlArrayItem("CategoryOptions")]
		public List<XmlHtmlReportDataQualityCategoryOptions> CategoryOptions { get; set; }

		// TODO filter criteria for issues?
	}
}
