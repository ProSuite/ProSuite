using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public interface IQualityConditionObserver : IViewObserver
	{
		void OnTestDescriptorChanged();

		void SetTestParameterValues([NotNull] IList<TestParameterValue> values);

		[CanBeNull]
		ITestConfigurator GetTestConfigurator();

		[NotNull]
		BindingList<ParameterValueListItem> GetTestParameterItems();

		void ExportQualityCondition([NotNull] string exportFileName);

		void ImportQualityCondition([NotNull] string importFileName);

		void AssignToQualitySpecificationsClicked();

		void RemoveFromQualitySpecificationsClicked();

		void QualitySpecificationSelectionChanged();

		void IssueFilterSelectionChanged();

		void AddIssueFilterClicked();

		void RemoveIssueFilterClicked();

		void QualitySpecificationReferenceDoubleClicked(
			[NotNull] QualitySpecificationReferenceTableRow
				qualitySpecificationReferenceTableRow);

		void IssueFilterDoubleClicked(
			[NotNull] InstanceConfigurationReferenceTableRow filterConfigTableRow);

		void GoToTestDescriptorClicked([CanBeNull] TestDescriptor testDescriptor);

		void OpenUrlClicked();

		void NewVersionUuidClicked();

		void DescriptorDocumentationLinkClicked();

		[CanBeNull]
		string GenerateName();
	}
}
