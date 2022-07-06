using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.Controls;

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

		bool RemoveFromQualitySpecificationsEnabled { get; set; }

		int FirstQualitySpecificationReferenceIndex { get; }

		bool TestDescriptorLinkEnabled { get; set; }

		string QualitySpecificationSummary { get; set; }

		IInstanceConfigurationTableViewControl TableViewControl { get; }

		void BindToQualitySpecificationReferences(
			[NotNull] IList<QualitySpecificationReferenceTableRow> tableRows);

		[NotNull]
		IList<QualitySpecificationReferenceTableRow>
			GetSelectedQualitySpecificationReferenceTableRows();

		bool Confirm([NotNull] string message, [NotNull] string title);

		void UpdateScreen();

		void RenderCategory([CanBeNull] string categoryText);

		void SaveState();

		void SelectQualitySpecifications(
			[NotNull] IEnumerable<QualitySpecification> specsToSelect);
	}
}
