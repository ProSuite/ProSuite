using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public interface IDataGridViewFindToolsView
	{
		bool ClearFilterEnabled { get; set; }

		bool MoveNextEnabled { get; set; }

		bool MovePreviousEnabled { get; set; }

		string FindResultStatusText { get; set; }

		IDataGridViewFindObserver Observer { get; set; }

		bool FilterRowsButtonVisible { get; set; }

		[CanBeNull]
		string FindText { get; set; }

		bool FilterRows { get; set; }

		void SetFindResultStatusColor(Color color);

		void ClearFindResultStatusColor();

		void DisplayFilteredRowsState(bool hasFilteredRows);
	}
}
