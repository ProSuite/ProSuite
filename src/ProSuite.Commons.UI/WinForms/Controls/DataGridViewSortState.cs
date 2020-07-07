using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Class to retrieve, store and re-apply the sorting of a data grid view
	/// </summary>
	/// <remarks>The sorted column must have a unique name for this to work.</remarks>
	public class DataGridViewSortState : ColumnSortState
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewSortState"/> class.
		/// </summary>
		/// <remarks>Required for xml serialization</remarks>
		[UsedImplicitly]
		public DataGridViewSortState() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewSortState"/> class.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		public DataGridViewSortState([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			GetState(dataGridView);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewSortState"/> class.
		/// </summary>
		/// <param name="sortedColumnName">Name of the sorted column.</param>
		/// <param name="listSortDirection">The list sort direction.</param>
		public DataGridViewSortState(
			[CanBeNull] string sortedColumnName,
			ListSortDirection listSortDirection = ListSortDirection.Ascending)
			: base(sortedColumnName, listSortDirection) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewSortState"/> class.
		/// </summary>
		/// <param name="sortedColumnName">Name of the sorted column.</param>
		/// <param name="sortOrder">The sort order.</param>
		public DataGridViewSortState([CanBeNull] string sortedColumnName,
		                             SortOrder sortOrder)
			: base(sortedColumnName, sortOrder) { }

		#endregion

		public bool TrySortBindingList<T>([NotNull] IBindingList bindingList,
		                                  [NotNull] DataGridView dataGridView)
			where T : class
		{
			var listSortDirection = GetListSortDirection();

			if (listSortDirection == null || SortedColumnName == null)
			{
				return false;
			}

			return DataGridViewUtils.TrySortBindingList<T>(
				bindingList, dataGridView, SortedColumnName, listSortDirection.Value);
		}

		public bool TryApplyState([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			return DataGridViewUtils.TryApplySortState(dataGridView, this);
		}

		private void GetState([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			DataGridViewColumn sortedColumn = dataGridView.SortedColumn;
			if (sortedColumn == null)
			{
				return;
			}

			SortedColumnName = sortedColumn.Name;
			SortOrder = dataGridView.SortOrder;
		}
	}
}
