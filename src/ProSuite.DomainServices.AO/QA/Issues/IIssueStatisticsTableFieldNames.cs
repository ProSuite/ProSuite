using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueStatisticsTableFieldNames
	{
		[NotNull]
		string IssueDescriptionField { get; }

		[NotNull]
		string IssueCodeField { get; }

		[NotNull]
		string IssueCodeDescriptionField { get; }

		[NotNull]
		string QualityConditionField { get; }

		[NotNull]
		string QualityConditionDescriptionField { get; }

		[NotNull]
		string TestNameField { get; }

		[NotNull]
		string TestDescriptionField { get; }

		[NotNull]
		string TestTypeField { get; }

		[NotNull]
		string IssueTypeField { get; }

		[NotNull]
		string StopConditionField { get; }

		[NotNull]
		string CategoriesField { get; }

		[NotNull]
		string AffectedComponentField { get; }

		[NotNull]
		string IssueCountField { get; }

		[NotNull]
		string UrlField { get; }
	}
}
