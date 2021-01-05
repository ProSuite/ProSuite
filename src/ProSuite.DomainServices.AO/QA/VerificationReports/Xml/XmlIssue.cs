using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlIssue
	{
		[XmlAttribute("description")]
		public string Description { get; set; }

		[XmlAttribute("code")]
		[DefaultValue(null)]
		public string IssueCode { get; set; }

		[XmlAttribute("affectedComponent")]
		[DefaultValue(null)]
		public string AffectedComponent { get; set; }

		[XmlArray("Objects")]
		[XmlArrayItem("Table")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlInvolvedTable> InvolvedTables { get; set; }

		public void AddInvolvedTable([NotNull] XmlInvolvedTable xmlInvolvedTable)
		{
			if (InvolvedTables == null)
			{
				InvolvedTables = new List<XmlInvolvedTable>();
			}

			InvolvedTables.Add(xmlInvolvedTable);
		}

		[XmlElement("Extent")]
		[CanBeNull]
		public XmlEnvelope Extent { get; set; }
	}
}
