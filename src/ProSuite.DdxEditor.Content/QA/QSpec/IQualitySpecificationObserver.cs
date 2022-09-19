using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualitySpecificationObserver : IViewObserver
	{
		void AddQualityConditionsClicked();

		void RemoveElementsClicked();

		void AssignToCategoryClicked();

		void BinderChanged();

		void ElementSelectionChanged();

		void ElementDoubleClicked(
			[NotNull] QualitySpecificationElementTableRow qualitySpecificationElementTableRow);

		void OnElementsChanged();

		void CreateCopyOfQualitySpecification();

		void OpenUrlClicked();
	}
}
