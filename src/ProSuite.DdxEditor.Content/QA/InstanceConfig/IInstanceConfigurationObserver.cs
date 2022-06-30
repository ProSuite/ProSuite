using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationObserver : IViewObserver
	{
		void OnInstanceDescriptorChanged();

		void SetTestParameterValues([NotNull] IList<TestParameterValue> values);

		[NotNull]
		BindingList<ParameterValueListItem> GetTestParameterItems();

		// TODO: Remove
		void AssignToQualitySpecificationsClicked();

		void RemoveFromQualitySpecificationsClicked();

		void QualitySpecificationSelectionChanged();

		void QualitySpecificationReferenceDoubleClicked(
			[NotNull] QualitySpecificationReferenceTableRow
				qualitySpecificationReferenceTableRow);

		void InstanceDescriptorLinkClicked([CanBeNull] InstanceDescriptor instanceDescriptor);

		void OpenUrlClicked();

		void NewVersionUuidClicked();
	}
}
