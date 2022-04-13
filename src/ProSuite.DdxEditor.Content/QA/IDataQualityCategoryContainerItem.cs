using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.Categories;

namespace ProSuite.DdxEditor.Content.QA
{
	public interface IDataQualityCategoryContainerItem
	{
		void AddNewDataQualityCategoryItem();

		bool AssignToCategory([NotNull] DataQualityCategoryItem item,
		                      [NotNull] IWin32Window owner,
		                      [CanBeNull] out DataQualityCategory category);

		[CanBeNull]
		DataQualityCategory GetDataQualityCategory([NotNull] DataQualityCategoryItem item);
	}
}
