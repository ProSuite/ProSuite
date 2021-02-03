using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlDatasetKeyField
	{
		[CanBeNull]
		[XmlAttribute("name")]
		public string DatasetName { get; set; }

		[CanBeNull]
		[XmlAttribute("keyField")]
		public string KeyField { get; set; }
	}
}