using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public interface IInstanceDescriptorObserver : IViewObserver
	{
		void NotifyFactoryChanged();

		void InstanceConfigurationDoubleClicked(
			ReferencingInstanceConfigurationTableRow referencingQualityConditionTableRow);
	}
}
