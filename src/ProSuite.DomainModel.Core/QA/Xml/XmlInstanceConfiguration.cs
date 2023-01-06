using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlInstanceConfiguration : IXmlEntityMetadata
	{
		[CanBeNull] private string _description;
		[CanBeNull] private string _notes;
		[CanBeNull] private string _url;

		[NotNull] private readonly List<XmlTestParameterValue> _parameterValues =
			new List<XmlTestParameterValue>();

		[CanBeNull]
		[XmlAttribute("name")]
		public string Name { get; set; }

		[NotNull]
		[XmlArray("Parameters", Order = 6)]
		[XmlArrayItem(typeof(XmlDatasetTestParameterValue), ElementName = "Dataset")]
		[XmlArrayItem(typeof(XmlScalarTestParameterValue), ElementName = "Scalar")]
		public List<XmlTestParameterValue> ParameterValues => _parameterValues;

		[CanBeNull]
		[XmlElement("Description", Order = 2)]
		[DefaultValue(null)]
		public string Description
		{
			get => string.IsNullOrEmpty(_description)
				       ? null
				       : _description;
			set => _description = value;
		}

		[CanBeNull]
		[XmlElement("Notes", Order = 4)]
		[DefaultValue(null)]
		public string Notes
		{
			get => string.IsNullOrEmpty(_notes)
				       ? null
				       : _notes;
			set => _notes = value;
		}

		[CanBeNull]
		[XmlAttribute("url")]
		public string Url
		{
			get => string.IsNullOrEmpty(_url)
				       ? null
				       : _url;
			set => _url = value;
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
