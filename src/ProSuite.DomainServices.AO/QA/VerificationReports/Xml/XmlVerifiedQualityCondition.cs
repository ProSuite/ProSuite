using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlVerifiedQualityCondition
	{
		[XmlAttribute("name")]
		[UsedImplicitly]
		public string Name { get; set; }

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

		[XmlElement("Description")]
		[UsedImplicitly]
		public string Description { get; set; }

		[XmlElement("Test")]
		[UsedImplicitly]
		public XmlTestDescriptor TestDescriptor { get; set; }

		[XmlArray("Parameters")]
		[XmlArrayItem("Parameter")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlTestParameterValue> ParameterValues { get; set; }

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

		public void AddParameters(
			[NotNull] IEnumerable<XmlTestParameterValue> parameterValues)
		{
			Assert.ArgumentNotNull(parameterValues, nameof(parameterValues));

			if (ParameterValues == null)
			{
				ParameterValues = new List<XmlTestParameterValue>();
			}

			ParameterValues.AddRange(parameterValues);
		}
	}
}
