using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public interface IInstanceDescriptorObserver : IViewObserver
	{
		void NotifyFactoryChanged();

		void InstanceConfigurationDoubleClicked(
			ReferencingInstanceConfigurationTableRow referencingQualityConditionTableRow);
	}
}
