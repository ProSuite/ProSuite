using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualitySpecificationView :
		IBoundView<QualitySpecification, IQualitySpecificationObserver>
	{
		bool HasSelectedElements { get; }

		int LastSelectedElementIndex { get; }

		bool RemoveElementsEnabled { get; set; }

		bool AssignToCategoryEnabled { get; set; }

		bool HasSingleSelectedElement { get; }

		int ElementCount { get; }

		[NotNull]
		IList<QualitySpecificationElementTableRow> GetSelectedElementTableRows();

		void RefreshElements();

		void BindToElements([NotNull] IList<QualitySpecificationElementTableRow> tableRows);

		void SelectElements(
			[NotNull] IEnumerable<QualitySpecificationElement> elementsToSelect);

		[NotNull]
		IList<QualitySpecificationElement> GetSelectedElements();

		void RenderCategory([CanBeNull] string categoryText);

		void SaveState();
	}
}
