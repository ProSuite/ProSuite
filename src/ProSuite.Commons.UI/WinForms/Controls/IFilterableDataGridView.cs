using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public interface IFilterableDataGridView
	{
		/// <summary>
		/// Filters rows in the <see cref="DataGridView"></see>. Invokes a callback function that
		/// determines if a given row should be visible or not.
		/// </summary>
		/// <param name="isRowVisible">Callback for returning the visible state for a given row.</param>
		void FilterRows([NotNull] Func<DataGridViewRow, bool> isRowVisible);

		void ShowAllRows();
	}
}
