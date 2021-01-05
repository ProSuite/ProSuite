using System;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	[Flags]
	public enum IssueReportingContexts
	{
		None = 0,
		VerifiedQualityCondition = 1,
		Dataset = 2,
		QualityConditionWithIssues = 4
	}
}
