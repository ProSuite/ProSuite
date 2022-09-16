using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public class XmlAttributeValueMapping
	{
		[XmlAttribute("sourceText")]
		public string SourceText { get; set; }

		[XmlAttribute("targetText")]
		public string TargetText { get; set; }

		[XmlAttribute("description")]
		public string Description { get; set; }

		public override string ToString()
		{
			return string.Format("{0} => {1}", SourceText, TargetText);
		}
	}
}
