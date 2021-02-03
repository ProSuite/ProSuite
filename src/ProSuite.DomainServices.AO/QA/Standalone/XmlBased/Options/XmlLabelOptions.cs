using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlLabelOptions
	{
		[XmlAttribute("visible")]
		public TrueFalseDefault Visible { get; set; } = TrueFalseDefault.@default;

		[XmlAttribute("minimumScale")]
		public double MinimumScale { get; set; }

		[XmlAttribute("isExpressionSimple")]
		public bool IsExpressionSimple { get; set; } = true;

		[XmlText]
		public string Expression { get; set; }
	}
}