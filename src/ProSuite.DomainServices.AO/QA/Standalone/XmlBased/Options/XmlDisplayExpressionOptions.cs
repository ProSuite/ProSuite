using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlDisplayExpressionOptions
	{
		[XmlAttribute("showMapTips")]
		public TrueFalseDefault ShowMapTips { get; set; } = TrueFalseDefault.@default;

		[XmlAttribute("isExpressionSimple")]
		public bool IsExpressionSimple { get; set; } = true;

		[XmlText]
		public string Expression { get; set; }
	}
}
