using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class FieldStatistics
	{
		[XmlAttribute("minimumValue")]
		public string MinimumValue { get; set; }

		[XmlAttribute("maximumValue")]
		public string MaximumValue { get; set; }

		[XmlAttribute("nullValueCount")]
		public int NullValueCount { get; set; }

		[XmlAttribute("valueCount")]
		public int ValueCount { get; set; }

		[CanBeNull]
		[XmlElement("DistinctValues")]
		public FieldDistinctValues DistinctValues { get; set; }
	}
}
