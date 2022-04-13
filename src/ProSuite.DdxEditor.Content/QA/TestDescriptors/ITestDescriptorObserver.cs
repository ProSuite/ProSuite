using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public interface ITestDescriptorObserver : IViewObserver
	{
		void NotifyFactoryChanged();

		void QualityConditionDoubleClicked(
			ReferencingQualityConditionTableRow referencingQualityConditionTableRow);
	}
}