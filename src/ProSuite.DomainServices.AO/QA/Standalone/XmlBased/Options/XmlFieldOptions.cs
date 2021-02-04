using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlFieldOptions
	{
		[XmlAttribute("field")]
		public string Field { get; set; }

		[XmlAttribute("aliasName")]
		public string AliasName { get; set; }

		[XmlAttribute("visible")]
		public TrueFalseDefault Visible { get; set; } = TrueFalseDefault.@default;

		[XmlAttribute("highlight")]
		public TrueFalseDefault Highlight { get; set; } = TrueFalseDefault.@default;

		[XmlAttribute("readOnly")]
		public TrueFalseDefault ReadOnly { get; set; } = TrueFalseDefault.@default;
	}
}
