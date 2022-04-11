using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IItemTableObserver<T> where T : class
	{
		void RowDoubleClicked([NotNull] T row);

		void RowRightClicked([NotNull] T row, Point location);
	}
}
