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
		[XmlArray("Parameters", Order = 6)]
		[XmlArrayItem(typeof(XmlDatasetTestParameterValue), ElementName = "Dataset")]
		[XmlArrayItem(typeof(XmlScalarTestParameterValue), ElementName = "Scalar")]
		public List<XmlTestParameterValue> ParameterValues
		{
			get { return _parameterValues; }
		}

		[CanBeNull]
		[XmlElement("Description", Order = 2)]
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
}
