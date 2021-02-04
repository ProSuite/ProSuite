using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlIssueMapOptions
	{
		[CanBeNull]
		[XmlAttribute("template")]
		public string TemplatePath { get; set; }

		[CanBeNull]
		[XmlAttribute("mxdFile")]
		public string MxdFileName { get; set; }

		[XmlAttribute("listLayersByAffectedComponent")]
		[DefaultValue(true)]
		public bool ListLayersByAffectedComponent { get; set; } = true;

		[XmlAttribute("issueLayersGroupBy")]
		[DefaultValue(IssueLayersGroupBy.IssueType)]
		public IssueLayersGroupBy IssueLayersGroupBy { get; set; } =
			IssueLayersGroupBy.IssueType;

		[CanBeNull]
		[XmlAttribute("version")]
		public string Version { get; set; }

		[XmlAttribute("displayLabels")]
		public bool DisplayLabels { get; set; } = true;

		[XmlAttribute("showMapTips")]
		public bool ShowMapTips { get; set; } = true;

		[XmlAttribute("verifiedFeaturesMinimumScale")]
		public double VerifiedFeaturesMinimumScale { get; set; } = 50000;

		[XmlElement("IssueLabels")]
		public XmlLabelOptions IssueLabelOptions { get; set; }

		[XmlElement("IssueDisplayExpression")]
		public XmlDisplayExpressionOptions IssueDisplayExpression { get; set; }

		[XmlElement("ExceptionLabels")]
		public XmlLabelOptions ExceptionLabelOptions { get; set; }

		[XmlElement("ExceptionDisplayExpression")]
		public XmlDisplayExpressionOptions ExceptionDisplayExpression { get; set; }

		[XmlArray("IssueFieldOptions")]
		[XmlArrayItem("Field")]
		[CanBeNull]
		public List<XmlFieldOptions> IssueFieldOptions { get; set; }

		[XmlArray("ExceptionFieldOptions")]
		[XmlArrayItem("Field")]
		[CanBeNull]
		public List<XmlFieldOptions> ExceptionFieldOptions { get; set; }
	}
}
