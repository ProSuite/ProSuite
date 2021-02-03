using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlQualitySpecificationElement
	{
		[XmlAttribute("qualityCondition")]
		public string QualityConditionName { get; set; }

		[XmlAttribute("allowErrors")]
		[DefaultValue(Override.Null)]
		public Override AllowErrors { get; set; }

		[XmlAttribute("stopOnError")]
		[DefaultValue(Override.Null)]
		public Override StopOnError { get; set; }
	}
}