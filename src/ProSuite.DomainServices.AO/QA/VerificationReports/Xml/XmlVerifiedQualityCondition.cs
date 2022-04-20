using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlVerifiedQualityCondition : XmlInstanceConfiguration
	{
		[XmlAttribute("uuid")]
		[UsedImplicitly]
		public string Guid { get; set; }

		[XmlAttribute("versionUuid")]
		[UsedImplicitly]
		public string VersionGuid { get; set; }

		[XmlAttribute("type")]
		[UsedImplicitly]
		public XmlQualityConditionType Type { get; set; }

		[XmlAttribute("url")]
		[UsedImplicitly]
		public string Url { get; set; }

		[XmlAttribute("stopCondition")]
		[UsedImplicitly]
		public bool StopCondition { get; set; }

		[XmlAttribute("issueCount")]
		[UsedImplicitly]
		public int IssueCount { get; set; }

		[XmlAttribute("exceptionCount")]
		[UsedImplicitly]
		[DefaultValue(0)]
		public int ExceptionCount { get; set; }

		[XmlArray("Issues")]
		[XmlArrayItem("Issue")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlIssue> Issues { get; set; }

		[XmlIgnore]
		[CanBeNull]
		public DataQualityCategory Category { get; set; }

		public void AddIssues([NotNull] ICollection<XmlIssue> issues,
		                      bool reportIndividualIssues)
		{
			Assert.ArgumentNotNull(issues, nameof(issues));

			if (reportIndividualIssues)
			{
				if (Issues == null)
				{
					Issues = new List<XmlIssue>();
				}

				Issues.AddRange(issues);

				IssueCount = Issues.Count;
			}
			else
			{
				IssueCount += issues.Count;
			}
		}
	}
}
