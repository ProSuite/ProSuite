using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlInstanceConfiguration
	{
		[CanBeNull] private string _description;

		[NotNull] private readonly List<XmlTestParameterValue> _parameterValues =
			new List<XmlTestParameterValue>();

		[CanBeNull]
		[XmlAttribute("name")]
		public string Name { get; set; }

		[NotNull]
		[XmlArray("Parameters")]
		[XmlArrayItem(typeof(XmlDatasetTestParameterValue), ElementName = "Dataset")]
		[XmlArrayItem(typeof(XmlScalarTestParameterValue), ElementName = "Scalar")]
		public List<XmlTestParameterValue> ParameterValues
		{
			get { return _parameterValues; }
		}

		[CanBeNull]
		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get
			{
				return string.IsNullOrEmpty(_description)
					       ? null
					       : _description;
			}
			set { _description = value; }
		}

		public IEnumerable<XmlTestParameterValue> EnumParameterValues(bool ignoreEmptyValues = true)
		{
			foreach (XmlTestParameterValue paramValue in _parameterValues)
			{
				if (ignoreEmptyValues &&
				    paramValue is XmlDatasetTestParameterValue dsValue &&
				    dsValue.IsEmpty())
				{
					continue;
				}

				yield return paramValue;
			}
		}

		[CanBeNull]
		[XmlAttribute("uuid")]
		public string Uuid { get; set; }

		[CanBeNull]
		[XmlAttribute("versionUuid")]
		public string VersionUuid { get; set; }
	}

	public class XmlRowFilterConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("rowFilterDescriptor")]
		public string RowFilterDescriptorName { get; set; }
	}

	public class XmlIssueFilterConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("issueFilterDescriptor")]
		public string IssueFilterDescriptorName { get; set; }
	}

	public class XmlTransformerConfiguration : XmlInstanceConfiguration
	{
		[CanBeNull]
		[XmlAttribute("transformerDescriptor")]
		public string TransformerDescriptorName { get; set; }
	}

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
		[XmlElement("Notes")]
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
