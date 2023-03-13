using System.Collections.Generic;
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
		[XmlArray("Filters", Order = 8)]
		[XmlArrayItem("Filter")]
		public List<XmlFilter> Filters { get; set; }

		[CanBeNull]
		[XmlElement(Order = 9)]
		[DefaultValue(null)]
		public XmlFilterExpression FilterExpression { get; set; }
	}
}
