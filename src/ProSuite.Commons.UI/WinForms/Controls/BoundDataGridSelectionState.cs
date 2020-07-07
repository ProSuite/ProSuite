using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Stores the state of a table row selection. Used by <see cref="BoundDataGridHandler{ROW}"/> 
	/// to get and restore a selection.
	/// </summary>
	/// <typeparam name="ROW"></typeparam>
	public class BoundDataGridSelectionState<ROW> where ROW : class
	{
		[NotNull] private readonly HashSet<ROW> _selectedRows;

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundDataGridSelectionState&lt;ROW&gt;"/> class.
		/// </summary>
		/// <param name="selectedRows">The selected rows.</param>
		public BoundDataGridSelectionState([NotNull] HashSet<ROW> selectedRows)
		{
			Assert.ArgumentNotNull(selectedRows, nameof(selectedRows));

			_selectedRows = selectedRows;
		}

		public int SelectionCount => _selectedRows.Count;

		public bool IsSelected(ROW tableRow)
		{
			return _selectedRows.Contains(tableRow);
		}
	}
}
