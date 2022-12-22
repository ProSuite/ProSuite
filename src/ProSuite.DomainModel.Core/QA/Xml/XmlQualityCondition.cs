using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlQualityCondition : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("testDescriptor")]
		public string TestDescriptorName { get; set; }

		[XmlAttribute("allowErrors")]
		[DefaultValue(Override.Null)]
		public Override AllowErrors { get; set; }

		[XmlAttribute("stopOnError")]
		[DefaultValue(Override.Null)]
		public Override StopOnError { get; set; }

		[XmlAttribute("neverFilterTableRowsUsingRelatedGeometry")]
		[DefaultValue(false)]
		public bool NeverFilterTableRowsUsingRelatedGeometry { get; set; }

		[XmlAttribute("neverStoreRelatedGeometryForTableRowIssues")]
		[DefaultValue(false)]
		public bool NeverStoreRelatedGeometryForTableRowIssues { get; set; }

		[CanBeNull]
		[XmlElement(Order = 8)]
		public XmlFilterExpression IssueFilterExpression { get; set; }
	}
}
