using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlFilterExpression
	{
		[XmlAttribute("expression")]
		public string Expression { get; set; }
	}
}
