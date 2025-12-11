using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationObserver : IViewObserver
	{
		void OnInstanceDescriptorChanged();

		void SetTestParameterValues([NotNull] IList<TestParameterValue> values);

		[NotNull]
		BindingList<ParameterValueListItem> GetTestParameterItems();

		void InstanceReferenceDoubleClicked(
			[NotNull] InstanceConfigurationReferenceTableRow instanceReferenceTableRow);

		void GoToInstanceDescriptorClicked([CanBeNull] InstanceDescriptor instanceDescriptor);

		void OpenUrlClicked();

		void DescriptorDocumentationLinkClicked();

		[CanBeNull]
		string GenerateName();
	}
}
