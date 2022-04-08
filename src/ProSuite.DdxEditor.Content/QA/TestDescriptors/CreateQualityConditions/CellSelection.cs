using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal class CellSelection
	{
		private readonly Dictionary<string, List<DataGridViewCell>> _cellsPerColumn =
			new Dictionary<string, List<DataGridViewCell>>();

		private readonly DataGridView _dataGridView;
		private readonly int _cellCount;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CellSelection"/> class.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		public CellSelection([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			_dataGridView = dataGridView;

			foreach (DataGridViewCell cell in _dataGridView.SelectedCells)
			{
				Add(cell);
				_cellCount++;
			}
		}

		#endregion

		public int CellCount => _cellCount;

		public IList<string> GetColumnNames()
		{
			return new List<string>(_cellsPerColumn.Keys);
		}

		[NotNull]
		public IList<DataRow> GetRows([NotNull] string columnName)
		{
			var result = new List<DataRow>();

			List<DataGridViewCell> cells;
			if (_cellsPerColumn.TryGetValue(columnName, out cells))
			{
				foreach (DataGridViewCell cell in cells)
				{
					DataGridViewRow gridRow = _dataGridView.Rows[cell.RowIndex];

					result.Add(((DataRowView) gridRow.DataBoundItem).Row);
				}
			}

			cells.Sort(
				delegate(DataGridViewCell c1, DataGridViewCell c2)
				{
					if (c1 == null && c2 == null)
					{
						return -1;
					}

					return c1?.RowIndex.CompareTo(c2.RowIndex) ?? 1;
				});

			return result;
		}

		public bool IsRectangular()
		{
			var columnNames = new List<string>(_cellsPerColumn.Keys);
			if (columnNames.Count == 0)
			{
				return false;
			}

			IList<DataRow> firstColumnRows = GetRows(columnNames[0]);

			for (var columnIndex = 1; columnIndex < columnNames.Count; columnIndex++)
			{
				IList<DataRow> columnRows = GetRows(columnNames[columnIndex]);

				if (columnRows.Count != firstColumnRows.Count)
				{
					return false;
				}

				for (var rowIndex = 0; rowIndex < columnRows.Count; rowIndex++)
				{
					if (columnRows[rowIndex] != firstColumnRows[rowIndex])
					{
						return false;
					}
				}
			}

			return true;
		}

		private void Add([NotNull] DataGridViewCell cell)
		{
			string columnName = _dataGridView.Columns[cell.ColumnIndex].DataPropertyName;

			List<DataGridViewCell> cells;
			if (! _cellsPerColumn.TryGetValue(columnName, out cells))
			{
				cells = new List<DataGridViewCell>();
				_cellsPerColumn.Add(columnName, cells);
			}

			Assert.False(cells.Contains(cell), "duplicate cell");

			cells.Add(cell);
		}
	}
}
