using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class DataGridViewFindResultRow
	{
		private readonly HashSet<DataGridViewFindResultCell> _cells;

		public DataGridViewFindResultRow()
		{
			_cells = new HashSet<DataGridViewFindResultCell>();
		}

		public bool HasFindResultInCell(int columnIndex)
		{
			return TryGetCell(columnIndex, out _);
		}

		public bool TryGetCell(int columnIndex, out DataGridViewFindResultCell foundCell)
		{
			foreach (DataGridViewFindResultCell findResultCell in _cells)
			{
				if (findResultCell.ColumnIndex != columnIndex)
				{
					continue;
				}

				foundCell = findResultCell;
				return true;
			}

			foundCell = null;
			return false;
		}

		public void Add([NotNull] DataGridViewFindResultCell findCell)
		{
			_cells.Add(findCell);
		}

		public int FindResultCount => _cells.Count;
	}
}
