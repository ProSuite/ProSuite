using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlInstanceConfiguration
	{
		[XmlAttribute("name")]
		[UsedImplicitly]
		public string Name { get; set; }

		[XmlElement("Description")]
		[UsedImplicitly]
		public string Description { get; set; }

		[XmlElement("Test")]
		[UsedImplicitly]
		public XmlTestDescriptor TestDescriptor { get; set; }

		[XmlArray("Parameters")]
		[XmlArrayItem("Parameter")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlTestParameterValue> ParameterValues { get; set; }

		public void AddParameters(
			[NotNull] IEnumerable<XmlTestParameterValue> parameterValues)
		{
			Assert.ArgumentNotNull(parameterValues, nameof(parameterValues));

			if (ParameterValues == null)
			{
				ParameterValues = new List<XmlTestParameterValue>();
			}

			ParameterValues.AddRange(parameterValues);
		}
	}
}
