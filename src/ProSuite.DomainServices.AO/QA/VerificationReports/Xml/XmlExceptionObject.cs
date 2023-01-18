using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlExceptionObject
	{
		[XmlAttribute("id")]
		[UsedImplicitly]
		public long Id { get; set; }

		[XmlAttribute("shapeType")]
		[UsedImplicitly]
		[CanBeNull]
		public string ShapeType { get; set; }

		[XmlAttribute("issueCode")]
		[UsedImplicitly]
		[CanBeNull]
		public string IssueCode { get; set; }

		[XmlAttribute("involvedObjects")]
		[UsedImplicitly]
		[CanBeNull]
		public string InvolvedObjects { get; set; }

		[XmlAttribute("usageCount")]
		[UsedImplicitly]
		[DefaultValue(0)]
		public int UsageCount { get; set; }
	}
}
