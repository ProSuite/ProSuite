using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlQualityCondition : XmlInstanceConfiguration, IXmlEntityMetadata
	{
		[CanBeNull] private string _notes;
		[CanBeNull] private string _url;

		[CanBeNull]
		[XmlAttribute("url")]
		public string Url
		{
			get
			{
				return string.IsNullOrEmpty(_url)
					       ? null
					       : _url;
			}
			set { _url = value; }
		}

		[CanBeNull]
		[XmlAttribute("testDescriptor")]
		public string TestDescriptorName { get; set; }

		[CanBeNull]
		[XmlElement("Notes", Order = 4)]
		[DefaultValue(null)]
		public string Notes
		{
			get
			{
				return string.IsNullOrEmpty(_notes)
					       ? null
					       : _notes;
			}
			set { _notes = value; }
		}

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

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }
	}
}
