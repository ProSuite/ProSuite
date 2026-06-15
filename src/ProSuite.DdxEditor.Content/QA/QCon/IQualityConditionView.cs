using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public interface IQualityConditionView :
		IBoundView<QualityCondition, IQualityConditionObserver>
	{
		Func<object> FindTestDescriptorDelegate { get; set; }

		void BindToParameterValues(
			[NotNull] BindingList<ParameterValueListItem> parameterValues);

		void SetTestDescription(string description);

		void SetParameterDescriptions([CanBeNull] IList<TestParameter> paramList);

		void SetConfigurator([CanBeNull] ITestConfigurator configurator);

		bool ExportEnabled { get; set; }

		bool ImportEnabled { get; set; }

		string IssueTypeDefault { get; set; }

		string StopOnErrorDefault { get; set; }

		bool HasSelectedQualitySpecificationReferences { get; }

		bool HasSelectedIssueFilter { get; }

		bool RemoveFromQualitySpecificationsEnabled { get; set; }

		bool RemoveIssueFilterEnabled { get; set; }

		int FirstQualitySpecificationReferenceIndex { get; }

		int FirstIssueFilterIndex { get; }

		bool GoToTestDescriptorEnabled { get; set; }

		string QualitySpecificationSummary { get; set; }

		IInstanceConfigurationTableViewControl TableViewControl { get; }

		void BindToQualitySpecificationReferences(
			[NotNull] IList<QualitySpecificationReferenceTableRow> tableRows);

		void BindToIssueFilters(
			[NotNull]
			SortableBindingList<InstanceConfigurationReferenceTableRow> issueFilterTableRows);

		[NotNull]
		IList<QualitySpecificationReferenceTableRow>
			GetSelectedQualitySpecificationReferenceTableRows();

		IList<InstanceConfigurationReferenceTableRow> GetSelectedIssueFilterTableRows();

		bool Confirm([NotNull] string message, [NotNull] string title);

		void UpdateScreen();

		void RenderCategory([CanBeNull] string categoryText);

		void SaveState();

		void SelectQualitySpecifications(
			[NotNull] IEnumerable<QualitySpecification> specsToSelect);

		void SelectIssueFilters(
			IEnumerable<IssueFilterConfiguration> filtersToSelect);
	}
}
