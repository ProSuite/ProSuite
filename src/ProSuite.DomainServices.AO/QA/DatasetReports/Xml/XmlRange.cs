using System.Xml.Serialization;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class XmlRange
	{
		[XmlAttribute("min")]
		public double Min { get; set; }

		[XmlAttribute("max")]
		public double Max { get; set; }
	}
}
