using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public static class IssueStatisticsTableFieldNamesFactory
	{
		[NotNull]
		public static IIssueStatisticsTableFieldNames GetFileGdbTableFieldNames()
		{
			return new IssueStatisticsTableFieldNames("Description",
			                                          "Code",
			                                          "CodeDescription",
			                                          "QualityCondition",
			                                          "QualityConditionDescription",
			                                          "TestName",
			                                          "TestDescription",
			                                          "TestType",
			                                          "IssueType",
			                                          "StopCondition",
			                                          "Categories",
			                                          "AffectedComponent",
			                                          "Url",
			                                          "IssueCount");
		}

		[NotNull]
		public static IIssueStatisticsTableFieldNames GetDbfTableFieldNames()
		{
			return new IssueStatisticsTableFieldNames("Descript",
			                                          "Code",
			                                          "CodeDesc",
			                                          "Condition",
			                                          "CondDescr",
			                                          "TestName",
			                                          "TestDesc",
			                                          "TestType",
			                                          "IssueType",
			                                          "StopCond",
			                                          "Categories",
			                                          "Component",
			                                          "Url",
			                                          "IssueCount");
		}
	}
}
