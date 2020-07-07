using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class DataGridViewFindResults
	{
		[NotNull]
		private readonly Dictionary<int, DataGridViewFindResultRow> _findResultRowsByIndex;

		private int _findCellIndex;

		public DataGridViewFindResults()
		{
			_findResultRowsByIndex = new Dictionary<int, DataGridViewFindResultRow>();
			FindResultRows = new List<DataGridViewFindResultRow>();
			FindResultCells = new List<DataGridViewFindResultCell>();
			_findCellIndex = 0;
		}

		public IList<DataGridViewFindResultRow> FindResultRows { get; }

		public IList<DataGridViewFindResultCell> FindResultCells { get; }

		public void Clear()
		{
			_findResultRowsByIndex.Clear();
			FindResultRows.Clear();
			FindResultCells.Clear();
			_findCellIndex = 0;
		}

		public void AddFindResult(int rowIndex, int columnIndex)
		{
			if (HasFindResult(rowIndex, columnIndex))
			{
				return;
			}

			DataGridViewFindResultRow foundRow;

			var foundCell = new DataGridViewFindResultCell(rowIndex, columnIndex,
			                                               _findCellIndex);

			if (! _findResultRowsByIndex.TryGetValue(rowIndex, out foundRow))
			{
				AddCellToNewRow(foundCell, rowIndex);
			}
			else
			{
				foundRow.Add(foundCell);
			}

			FindResultCells.Add(foundCell);

			_findCellIndex++;
		}

		public bool HasFindResult(int rowIndex)
		{
			return _findResultRowsByIndex.ContainsKey(rowIndex);
		}

		public bool HasFindResult(int rowIndex, int columnIndex)
		{
			DataGridViewFindResultRow foundRow;

			return _findResultRowsByIndex.TryGetValue(rowIndex, out foundRow) &&
			       foundRow.HasFindResultInCell(columnIndex);
		}

		public int GetFindCellIndex(int rowIndex, int columnIndex)
		{
			DataGridViewFindResultRow foundRow;

			if (_findResultRowsByIndex.TryGetValue(rowIndex, out foundRow) &&
			    foundRow.HasFindResultInCell(columnIndex))
			{
				DataGridViewFindResultCell cell;
				foundRow.TryGetCell(columnIndex, out cell);
				if (cell != null)
				{
					return cell.FindCellIndex;
				}
			}

			return -1;
		}

		private void AddCellToNewRow([NotNull] DataGridViewFindResultCell foundCell,
		                             int rowIndex)
		{
			var newRow = new DataGridViewFindResultRow();

			newRow.Add(foundCell);

			FindResultRows.Add(newRow);

			_findResultRowsByIndex.Add(rowIndex, newRow);
		}
	}
}
