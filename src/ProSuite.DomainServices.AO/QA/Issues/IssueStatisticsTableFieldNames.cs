using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueStatisticsTableFieldNames : IIssueStatisticsTableFieldNames
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IssueTableFields"/> class.
		/// </summary>
		/// <param name="issueDescriptionField">The error description field name.</param>
		/// <param name="issueCodeField">The issue code field.</param>
		/// <param name="issueCodeDescriptionField">The issue code description field.</param>
		/// <param name="qualityConditionField">The quality condition field name.</param>
		/// <param name="qualityConditionDescriptionField">The quality condition description field.</param>
		/// <param name="testNameField">The test name field name.</param>
		/// <param name="testDescriptionField">The test description field.</param>
		/// <param name="testTypeField">The test type field name.</param>
		/// <param name="issueTypeField">The error type field name.</param>
		/// <param name="stopConditionField">The stop condition field name.</param>
		/// <param name="categoriesField">The quality condition categories field name.</param>
		/// <param name="affectedComponentField">The affected property field name.</param>
		/// <param name="urlField">The URL field name.</param>
		/// <param name="issueCountField">The issue count field name.</param>
		public IssueStatisticsTableFieldNames(
			[NotNull] string issueDescriptionField,
			[NotNull] string issueCodeField,
			[NotNull] string issueCodeDescriptionField,
			[NotNull] string qualityConditionField,
			[NotNull] string qualityConditionDescriptionField,
			[NotNull] string testNameField,
			[NotNull] string testDescriptionField,
			[NotNull] string testTypeField,
			[NotNull] string issueTypeField,
			[NotNull] string stopConditionField,
			[NotNull] string categoriesField,
			[NotNull] string affectedComponentField,
			[NotNull] string urlField,
			[NotNull] string issueCountField)
		{
			Assert.ArgumentNotNullOrEmpty(issueDescriptionField,
			                              nameof(issueDescriptionField));
			Assert.ArgumentNotNullOrEmpty(issueCodeField, nameof(issueCodeField));
			Assert.ArgumentNotNullOrEmpty(issueCodeDescriptionField,
			                              nameof(issueCodeDescriptionField));
			Assert.ArgumentNotNullOrEmpty(qualityConditionField,
			                              nameof(qualityConditionField));
			Assert.ArgumentNotNullOrEmpty(qualityConditionDescriptionField,
			                              nameof(qualityConditionDescriptionField));
			Assert.ArgumentNotNullOrEmpty(testNameField, nameof(testNameField));
			Assert.ArgumentNotNullOrEmpty(testDescriptionField, nameof(testDescriptionField));
			Assert.ArgumentNotNullOrEmpty(testTypeField, nameof(testTypeField));
			Assert.ArgumentNotNullOrEmpty(issueTypeField, nameof(issueTypeField));
			Assert.ArgumentNotNullOrEmpty(stopConditionField, nameof(stopConditionField));
			Assert.ArgumentNotNullOrEmpty(categoriesField, nameof(categoriesField));
			Assert.ArgumentNotNullOrEmpty(affectedComponentField,
			                              nameof(affectedComponentField));
			Assert.ArgumentNotNullOrEmpty(urlField, nameof(urlField));
			Assert.ArgumentNotNullOrEmpty(issueCountField, nameof(issueCountField));

			IssueDescriptionField = issueDescriptionField;
			IssueCodeField = issueCodeField;
			IssueCodeDescriptionField = issueCodeDescriptionField;
			QualityConditionField = qualityConditionField;
			QualityConditionDescriptionField = qualityConditionDescriptionField;
			TestNameField = testNameField;
			TestDescriptionField = testDescriptionField;
			TestTypeField = testTypeField;
			IssueTypeField = issueTypeField;
			StopConditionField = stopConditionField;
			CategoriesField = categoriesField;
			AffectedComponentField = affectedComponentField;
			IssueCountField = issueCountField;
			UrlField = urlField;
		}

		public string IssueDescriptionField { get; }

		public string IssueCodeField { get; }

		public string IssueCodeDescriptionField { get; }

		public string QualityConditionField { get; }

		public string QualityConditionDescriptionField { get; }

		public string TestNameField { get; }

		public string TestDescriptionField { get; }

		public string TestTypeField { get; }

		public string IssueTypeField { get; }

		public string StopConditionField { get; }

		public string CategoriesField { get; }

		public string AffectedComponentField { get; }

		public string IssueCountField { get; }

		public string UrlField { get; }
	}
}
